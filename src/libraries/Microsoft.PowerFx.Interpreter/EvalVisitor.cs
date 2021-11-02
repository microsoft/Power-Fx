// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using System.Globalization;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx
{
    internal class EvalVisitor : IRNodeVisitor<FormulaValue, SymbolContext>
    {
        public CultureInfo CultureInfo { get; }

        public EvalVisitor(CultureInfo cultureInfo)
        {
            CultureInfo = cultureInfo;
        }

        // Helper to eval an arg that might be a lambda. 
        internal DValue<T> EvalArg<T>(FormulaValue arg, SymbolContext context, IRContext irContext) where T : ValidFormulaValue
        {
            if (arg is LambdaFormulaValue lambda)
            {
                var val = lambda.Eval(this, context);
                return val switch
                {
                    T t => DValue<T>.Of(t),
                    BlankValue b => DValue<T>.Of(b),
                    ErrorValue e => DValue<T>.Of(e),
                    _ => DValue<T>.Of(CommonErrors.RuntimeTypeMismatch(irContext))
                };
            }
            return arg switch
            {
                T t => DValue<T>.Of(t),
                BlankValue b => DValue<T>.Of(b),
                ErrorValue e => DValue<T>.Of(e),
                _ => DValue<T>.Of(CommonErrors.RuntimeTypeMismatch(irContext))
            };
        }

        public override FormulaValue Visit(TextLiteralNode node, SymbolContext context)
        {
            return new StringValue(node.IRContext, node.LiteralValue);
        }

        public override FormulaValue Visit(NumberLiteralNode node, SymbolContext context)
        {
            return new NumberValue(node.IRContext, node.LiteralValue);
        }

        public override FormulaValue Visit(BooleanLiteralNode node, SymbolContext context)
        {
            return new BooleanValue(node.IRContext, node.LiteralValue);
        }

        public override FormulaValue Visit(TableNode node, SymbolContext context)
        {
            // single-column table. 

            int len = node.Values.Count;

            // Were pushed left-to-right
            var args = new FormulaValue[len];
            for (int i = 0; i < len; i++)
            {
                var child = node.Values[i];
                var arg = child.Accept(this, context);
                args[i] = arg;
            }

            // Children are on the stack.
            var tableValue = new InMemoryTableValue(node.IRContext, Library.StandardTableNodeRecords(node.IRContext, args));

            return tableValue;
        }

        public override FormulaValue Visit(RecordNode node, SymbolContext context)
        {
            List<NamedValue> fields = new List<NamedValue>();

            foreach (var field in node.Fields)
            {
                var name = field.Key;
                IntermediateNode value = field.Value;

                FormulaValue rhsValue = value.Accept(this, context);
                fields.Add(new NamedValue(name.Value, rhsValue));
            }

            return new InMemoryRecordValue(node.IRContext, fields);
        }

        public override FormulaValue Visit(LazyEvalNode node, SymbolContext context)
        {
            var val = node.Child.Accept(this, context);
            return val;
        }

        public override FormulaValue Visit(CallNode node, SymbolContext context)
        {
            // Sum(  [1,2,3], Value * Value)            
            // return base.PreVisit(node);

            var func = node.Function;

            int carg = node.Args.Count;

            FormulaValue[] args = new FormulaValue[carg];

            for (int i = 0; i < carg; i++)
            {
                var child = node.Args[i];
                bool isLambda = node.IsLambdaArg(i);

                if (!isLambda)
                {
                    args[i] = child.Accept(this, context);
                }
                else
                {
                    args[i] = new LambdaFormulaValue(node.IRContext, child);
                }
            }

            var childContext = context.WithScope(node.Scope);

            if (func is CustomTexlFunction customFunc)
            {
                FormulaValue result = customFunc.Invoke(args);
                return result;
            }
            else
            {
                FunctionPtr ptr;
                if (FuncsByName.TryGetValue(func, out ptr))
                {
                    FormulaValue result = ptr(this, childContext, node.IRContext, args);

                    Contract.Assert(result.IRContext.ResultType == node.IRContext.ResultType || result is ErrorValue || result.IRContext.ResultType is BlankType);

                    return result;
                }
                return CommonErrors.NotYetImplementedError(node.IRContext, $"Missing func: {func.Name}");
            }
        }

        public override FormulaValue Visit(BinaryOpNode node, SymbolContext context)
        {
            var arg1 = node.Left.Accept(this, context);
            var arg2 = node.Right.Accept(this, context);
            var args = new FormulaValue[] { arg1, arg2 };

            switch (node.Op)
            {
                case BinaryOpKind.AddNumbers:
                    return Library.OperatorBinaryAdd(this, context, node.IRContext, args);
                case BinaryOpKind.MulNumbers:
                    return Library.OperatorBinaryMul(this, context, node.IRContext, args);
                case BinaryOpKind.DivNumbers:
                    return Library.OperatorBinaryDiv(this, context, node.IRContext, args);

                case BinaryOpKind.EqNumbers:
                case BinaryOpKind.EqBoolean:
                case BinaryOpKind.EqText:
                case BinaryOpKind.EqDate:
                case BinaryOpKind.EqTime:
                case BinaryOpKind.EqDateTime:
                case BinaryOpKind.EqHyperlink:
                case BinaryOpKind.EqCurrency:
                case BinaryOpKind.EqImage:
                case BinaryOpKind.EqColor:
                case BinaryOpKind.EqMedia:
                case BinaryOpKind.EqBlob:
                case BinaryOpKind.EqGuid:
                    return Library.OperatorBinaryEq(this, context, node.IRContext, args);

                case BinaryOpKind.NeqNumbers:
                case BinaryOpKind.NeqText:
                    return Library.OperatorBinaryNeq(this, context, node.IRContext, args);

                case BinaryOpKind.GtNumbers:
                    return Library.OperatorBinaryGt(this, context, node.IRContext, args);
                case BinaryOpKind.GeqNumbers:
                    return Library.OperatorBinaryGeq(this, context, node.IRContext, args);
                case BinaryOpKind.LtNumbers:
                    return Library.OperatorBinaryLt(this, context, node.IRContext, args);
                case BinaryOpKind.LeqNumbers:
                    return Library.OperatorBinaryLeq(this, context, node.IRContext, args);

                case BinaryOpKind.InText:
                    return Library.OperatorTextIn(this, context, node.IRContext, args);
                case BinaryOpKind.ExactInText:
                    return Library.OperatorTextInExact(this, context, node.IRContext, args);

                case BinaryOpKind.InScalarTable:
                    return Library.OperatorScalarTableIn(this, context, node.IRContext, args);

                case BinaryOpKind.ExactInScalarTable:
                    return Library.OperatorScalarTableInExact(this, context, node.IRContext, args);

                case BinaryOpKind.AddDateAndTime:
                    return Library.OperatorAddDateAndTime(this, context, node.IRContext, args);
                case BinaryOpKind.AddDateAndDay:
                    return Library.OperatorAddDateAndDay(this, context, node.IRContext, args);
                case BinaryOpKind.AddDateTimeAndDay:
                    return Library.OperatorAddDateTimeAndDay(this, context, node.IRContext, args);
                case BinaryOpKind.DateDifference:
                    return Library.OperatorDateDifference(this, context, node.IRContext, args);
                case BinaryOpKind.TimeDifference:
                    return Library.OperatorTimeDifference(this, context, node.IRContext, args);
                case BinaryOpKind.LtDateTime:
                    return Library.OperatorLtDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.LeqDateTime:
                    return Library.OperatorLeqDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.GtDateTime:
                    return Library.OperatorGtDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.GeqDateTime:
                    return Library.OperatorGeqDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.LtDate:
                    return Library.OperatorLtDate(this, context, node.IRContext, args);
                case BinaryOpKind.LeqDate:
                    return Library.OperatorLeqDate(this, context, node.IRContext, args);
                case BinaryOpKind.GtDate:
                    return Library.OperatorGtDate(this, context, node.IRContext, args);
                case BinaryOpKind.GeqDate:
                    return Library.OperatorGeqDate(this, context, node.IRContext, args);
                case BinaryOpKind.LtTime:
                    return Library.OperatorLtTime(this, context, node.IRContext, args);
                case BinaryOpKind.LeqTime:
                    return Library.OperatorLeqTime(this, context, node.IRContext, args);
                case BinaryOpKind.GtTime:
                    return Library.OperatorGtTime(this, context, node.IRContext, args);
                case BinaryOpKind.GeqTime:
                    return Library.OperatorGeqTime(this, context, node.IRContext, args);

                default:
                    return CommonErrors.UnreachableCodeError(node.IRContext);
            }
        }

        public override FormulaValue Visit(UnaryOpNode node, SymbolContext context)
        {
            var arg1 = node.Child.Accept(this, context);
            var args = new FormulaValue[] { arg1 };

            if (Library.UnaryOps.TryGetValue(node.Op, out Library.FunctionPtr unaryOp))
            {
                return unaryOp(this, context, node.IRContext, args);
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override FormulaValue Visit(ScopeAccessNode node, SymbolContext context)
        {
            if (node.Value is ScopeAccessSymbol s1)
            {
                ScopeSymbol scope = s1.Parent;

                var val = context.GetScopeVar(scope, s1.Name);
                return val;
            }
            if (node.Value is ScopeSymbol s2) // Binds to whole scope
            {
                IScope r = context.ScopeValues[s2.Id];
                var r2 = (RecordScope)r;
                return r2._context;
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override FormulaValue Visit(RecordFieldAccessNode node, SymbolContext context)
        {
            var left = node.From.Accept(this, context);

            if (left is BlankValue)
            {
                return new BlankValue(node.IRContext);
            }
            if(left is ErrorValue)
            {
                return left;
            }

            var record = (RecordValue)left;
            var val = record.GetField(node.IRContext, node.Field.Value);

            return val;
        }

        public override FormulaValue Visit(SingleColumnTableAccessNode node, SymbolContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Single column table access");
        }

        public override FormulaValue Visit(ErrorNode node, SymbolContext context)
        {
            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = node.ErrorHint,
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.AnalysisError
            });
        }

        public override FormulaValue Visit(ColorLiteralNode node, SymbolContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Color literal");
        }

        public override FormulaValue Visit(ChainingNode node, SymbolContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Expression chaining");
        }

        public override FormulaValue Visit(ResolvedObjectNode node, SymbolContext context)
        {
            if (node.Value is RecalcEngineResolver.ParameterData data)
            {
                var paramName = data.ParameterName;

                var value = context.Globals.GetField(node.IRContext, paramName);
                return value;
            }
            if (node.Value is RecalcFormulaInfo fi)
            {
                var value = fi._value;
                return value;
            }

            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = $"Unrecognized symbol {node?.Value?.GetType()?.Name}".Trim(),
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.Validation
            });
        }
    }
}
