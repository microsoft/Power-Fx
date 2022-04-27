// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Functions;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx
{
    // This used ValueTask for async, https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/ 
    // Perf comparison of Task vs. ValueTask: https://ladeak.wordpress.com/2019/03/09/valuetask-vs-task 
    // Use Task for public methods, but ValueTask for internal methods that we expect to be mostly sync. 
    internal class EvalVisitor : IRNodeVisitor<ValueTask<FormulaValue>, SymbolContext>
    {
        public CultureInfo CultureInfo { get; }

        private readonly CancellationToken _cancel;

        public EvalVisitor(CultureInfo cultureInfo, CancellationToken cancel)
        {
            CultureInfo = cultureInfo;
            _cancel = cancel;
        }
                
        // Check this cooperatively - especially in any loop. 
        public void CheckCancel()
        {
            // Throws OperationCanceledException exception
            _cancel.ThrowIfCancellationRequested();
        }

        // Helper to eval an arg that might be a lambda.
        internal async ValueTask<DValue<T>> EvalArgAsync<T>(FormulaValue arg, SymbolContext context, IRContext irContext)
            where T : ValidFormulaValue
        {
            if (arg is LambdaFormulaValue lambda)
            {
                var val = await lambda.EvalAsync(this, context);
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

        public override async ValueTask<FormulaValue> Visit(TextLiteralNode node, SymbolContext context)
        {
            return new StringValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(NumberLiteralNode node, SymbolContext context)
        {
            return new NumberValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(BooleanLiteralNode node, SymbolContext context)
        {
            return new BooleanValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(TableNode node, SymbolContext context)
        {
            var len = node.Values.Count;

            var args = new FormulaValue[len];
            for (var i = 0; i < len; i++)
            {
                CheckCancel();

                var child = node.Values[i];
                var arg = await child.Accept(this, context);
                args[i] = arg;
            }

            // This is always a single column table
            var tableValue = new InMemoryTableValue(node.IRContext, StandardTableNodeRecords(node.IRContext, args, forceSingleColumn: true));

            return tableValue;
        }

        public override async ValueTask<FormulaValue> Visit(RecordNode node, SymbolContext context)
        {
            var fields = new List<NamedValue>();

            foreach (var field in node.Fields)
            {
                CheckCancel();

                var name = field.Key;
                var value = field.Value;

                var rhsValue = await value.Accept(this, context);
                fields.Add(new NamedValue(name.Value, rhsValue));
            }

            return new InMemoryRecordValue(node.IRContext, fields);
        }

        public override async ValueTask<FormulaValue> Visit(LazyEvalNode node, SymbolContext context)
        {
            var val = await node.Child.Accept(this, context);
            return val;
        }

        public override async ValueTask<FormulaValue> Visit(CallNode node, SymbolContext context)
        {
            CheckCancel();

            // Sum(  [1,2,3], Value * Value)
            // return base.PreVisit(node);

            var func = node.Function;

            var carg = node.Args.Count;

            var args = new FormulaValue[carg];

            for (var i = 0; i < carg; i++)
            {
                CheckCancel();

                var child = node.Args[i];
                var isLambda = node.IsLambdaArg(i);

                if (!isLambda)
                {
                    args[i] = await child.Accept(this, context);
                }
                else
                {
                    args[i] = new LambdaFormulaValue(node.IRContext, child);
                }
            }

            var childContext = context.WithScope(node.Scope);

            if (func is IAsyncTexlFunction asyncFunc)
            {
                var result = await asyncFunc.InvokeAsync(args, _cancel);
                return result;
            }
            else if (func is CustomTexlFunction customFunc)
            {
                var result = customFunc.Invoke(args);
                return result;
            }
            else
            {
                if (FuncsByName.TryGetValue(func, out var ptr))
                {
                    var result = await ptr(this, childContext, node.IRContext, args);

                    Contract.Assert(result.IRContext.ResultType == node.IRContext.ResultType || result is ErrorValue || result.IRContext.ResultType is BlankType);

                    return result;
                }

                return CommonErrors.NotYetImplementedError(node.IRContext, $"Missing func: {func.Name}");
            }
        }

        public override async ValueTask<FormulaValue> Visit(BinaryOpNode node, SymbolContext context)
        {
            var arg1 = await node.Left.Accept(this, context);
            var arg2 = await node.Right.Accept(this, context);
            var args = new FormulaValue[] { arg1, arg2 };
            return await VisitBinaryOpNode(node, context, args);
        }

        private ValueTask<FormulaValue> VisitBinaryOpNode(BinaryOpNode node, SymbolContext context, FormulaValue[] args)
        { 
            switch (node.Op)
            {
                case BinaryOpKind.AddNumbers:
                    return OperatorBinaryAdd(this, context, node.IRContext, args);
                case BinaryOpKind.MulNumbers:
                    return OperatorBinaryMul(this, context, node.IRContext, args);
                case BinaryOpKind.DivNumbers:
                    return OperatorBinaryDiv(this, context, node.IRContext, args);
                case BinaryOpKind.EqBlob:

                case BinaryOpKind.EqBoolean:
                case BinaryOpKind.EqColor:
                case BinaryOpKind.EqCurrency:
                case BinaryOpKind.EqDate:
                case BinaryOpKind.EqDateTime:
                case BinaryOpKind.EqGuid:
                case BinaryOpKind.EqHyperlink:
                case BinaryOpKind.EqImage:
                case BinaryOpKind.EqMedia:
                case BinaryOpKind.EqNumbers:
                case BinaryOpKind.EqOptionSetValue:
                case BinaryOpKind.EqText:
                case BinaryOpKind.EqTime:
                    return OperatorBinaryEq(this, context, node.IRContext, args);

                case BinaryOpKind.NeqBlob:
                case BinaryOpKind.NeqBoolean:
                case BinaryOpKind.NeqColor:
                case BinaryOpKind.NeqCurrency:
                case BinaryOpKind.NeqDate:
                case BinaryOpKind.NeqDateTime:
                case BinaryOpKind.NeqGuid:
                case BinaryOpKind.NeqHyperlink:
                case BinaryOpKind.NeqImage:
                case BinaryOpKind.NeqMedia:
                case BinaryOpKind.NeqNumbers:
                case BinaryOpKind.NeqOptionSetValue:
                case BinaryOpKind.NeqText:
                case BinaryOpKind.NeqTime:
                    return OperatorBinaryNeq(this, context, node.IRContext, args);

                case BinaryOpKind.GtNumbers:
                    return OperatorBinaryGt(this, context, node.IRContext, args);
                case BinaryOpKind.GeqNumbers:
                    return OperatorBinaryGeq(this, context, node.IRContext, args);
                case BinaryOpKind.LtNumbers:
                    return OperatorBinaryLt(this, context, node.IRContext, args);
                case BinaryOpKind.LeqNumbers:
                    return OperatorBinaryLeq(this, context, node.IRContext, args);

                case BinaryOpKind.InText:
                    return OperatorTextIn(this, context, node.IRContext, args);
                case BinaryOpKind.ExactInText:
                    return OperatorTextInExact(this, context, node.IRContext, args);

                case BinaryOpKind.InScalarTable:
                    return OperatorScalarTableIn(this, context, node.IRContext, args);

                case BinaryOpKind.ExactInScalarTable:
                    return OperatorScalarTableInExact(this, context, node.IRContext, args);

                case BinaryOpKind.AddDateAndTime:
                    return OperatorAddDateAndTime(this, context, node.IRContext, args);
                case BinaryOpKind.AddDateAndDay:
                    return OperatorAddDateAndDay(this, context, node.IRContext, args);
                case BinaryOpKind.AddDateTimeAndDay:
                    return OperatorAddDateTimeAndDay(this, context, node.IRContext, args);
                case BinaryOpKind.DateDifference:
                    return OperatorDateDifference(this, context, node.IRContext, args);
                case BinaryOpKind.TimeDifference:
                    return OperatorTimeDifference(this, context, node.IRContext, args);
                case BinaryOpKind.LtDateTime:
                    return OperatorLtDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.LeqDateTime:
                    return OperatorLeqDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.GtDateTime:
                    return OperatorGtDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.GeqDateTime:
                    return OperatorGeqDateTime(this, context, node.IRContext, args);
                case BinaryOpKind.LtDate:
                    return OperatorLtDate(this, context, node.IRContext, args);
                case BinaryOpKind.LeqDate:
                    return OperatorLeqDate(this, context, node.IRContext, args);
                case BinaryOpKind.GtDate:
                    return OperatorGtDate(this, context, node.IRContext, args);
                case BinaryOpKind.GeqDate:
                    return OperatorGeqDate(this, context, node.IRContext, args);
                case BinaryOpKind.LtTime:
                    return OperatorLtTime(this, context, node.IRContext, args);
                case BinaryOpKind.LeqTime:
                    return OperatorLeqTime(this, context, node.IRContext, args);
                case BinaryOpKind.GtTime:
                    return OperatorGtTime(this, context, node.IRContext, args);
                case BinaryOpKind.GeqTime:
                    return OperatorGeqTime(this, context, node.IRContext, args);
                case BinaryOpKind.DynamicGetField:
                    return OperatorDynamicGetField(node, args);

                default:
                    return new ValueTask<FormulaValue>(CommonErrors.UnreachableCodeError(node.IRContext));
            }
        }

        private static async ValueTask<FormulaValue> OperatorDynamicGetField(BinaryOpNode node, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];

            if (arg1 is UntypedObjectValue cov && arg2 is StringValue sv)
            {
                if (cov.Impl.Type is ExternalType et && et.Kind == ExternalTypeKind.Object)
                {
                    if (cov.Impl.TryGetProperty(sv.Value, out var res))
                    {
                        if (res.Type == FormulaType.Blank)
                        {
                            return new BlankValue(node.IRContext);
                        }

                        return new UntypedObjectValue(node.IRContext, res);
                    }
                    else
                    {
                        return new BlankValue(node.IRContext);
                    }
                }
                else if (cov.Impl.Type == FormulaType.Blank)
                {
                    return new BlankValue(node.IRContext);
                }
                else
                {
                    return new ErrorValue(node.IRContext, new ExpressionError()
                    {
                        Message = "Accessing a field is not valid on this value",
                        Span = node.IRContext.SourceContext,
                        Kind = ErrorKind.BadLanguageCode
                    });
                }
            }
            else if (arg1 is BlankValue)
            {
                return new BlankValue(node.IRContext);
            }
            else if (arg1 is ErrorValue)
            {
                return arg1;
            }
            else
            {
                return CommonErrors.UnreachableCodeError(node.IRContext);
            }
        }

        public override async ValueTask<FormulaValue> Visit(UnaryOpNode node, SymbolContext context)
        {
            var arg1 = await node.Child.Accept(this, context);
            var args = new FormulaValue[] { arg1 };

            if (UnaryOps.TryGetValue(node.Op, out var unaryOp))
            {
                return await unaryOp(this, context, node.IRContext, args);
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override async ValueTask<FormulaValue> Visit(AggregateCoercionNode node, SymbolContext context)
        {
            var arg1 = await node.Child.Accept(this, context);

            if (node.Op == UnaryOpKind.TableToTable)
            {
                var table = (TableValue)arg1;
                var tableType = (TableType)node.IRContext.ResultType;
                var resultRows = new List<DValue<RecordValue>>();
                foreach (var row in table.Rows)
                {
                    CheckCancel();

                    if (row.IsValue)
                    {
                        var fields = new List<NamedValue>();
                        var scopeContext = context.WithScope(node.Scope);
                        foreach (var coercion in node.FieldCoercions)
                        {
                            CheckCancel();

                            var record = row.Value;
                            var newScope = scopeContext.WithScopeValues(record);

                            var newValue = await coercion.Value.Accept(this, newScope);
                            var name = coercion.Key;
                            fields.Add(new NamedValue(name.Value, newValue));
                        }

                        resultRows.Add(DValue<RecordValue>.Of(new InMemoryRecordValue(IRContext.NotInSource(tableType.ToRecord()), fields)));
                    }
                    else if (row.IsBlank)
                    {
                        resultRows.Add(DValue<RecordValue>.Of(row.Blank));
                    }
                    else
                    {
                        resultRows.Add(DValue<RecordValue>.Of(row.Error));
                    }
                }

                return new InMemoryTableValue(node.IRContext, resultRows);
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override async ValueTask<FormulaValue> Visit(ScopeAccessNode node, SymbolContext context)
        {
            if (node.Value is ScopeAccessSymbol s1)
            {
                var scope = s1.Parent;

                var val = context.GetScopeVar(scope, s1.Name);
                return val;
            }

            // Binds to whole scope
            if (node.Value is ScopeSymbol s2)
            {
                var r = context.ScopeValues[s2.Id];
                var r2 = (RecordScope)r;
                return r2._context;
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override async ValueTask<FormulaValue> Visit(RecordFieldAccessNode node, SymbolContext context)
        {
            var left = await node.From.Accept(this, context);

            if (left is BlankValue)
            {
                return new BlankValue(node.IRContext);
            }

            if (left is ErrorValue)
            {
                return left;
            }

            var record = (RecordValue)left;
            var val = record.GetField(node.IRContext.ResultType, node.Field.Value);

            return val;
        }

        public override async ValueTask<FormulaValue> Visit(SingleColumnTableAccessNode node, SymbolContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Single column table access");
        }

        public override async ValueTask<FormulaValue> Visit(ErrorNode node, SymbolContext context)
        {
            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = node.ErrorHint,
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.AnalysisError
            });
        }

        public override async ValueTask<FormulaValue> Visit(ColorLiteralNode node, SymbolContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Color literal");
        }

        public override async ValueTask<FormulaValue> Visit(ChainingNode node, SymbolContext context)
        {
            CheckCancel();

            if (!node.Nodes.Any())
            {
                return CommonErrors.InvalidChain(node.IRContext, node.ToString());
            }

            FormulaValue fv = null;

            foreach (var iNode in node.Nodes)
            {
                CheckCancel();

                fv = await iNode.Accept(this, context);
            }

            return fv;
        }

        public override async ValueTask<FormulaValue> Visit(ResolvedObjectNode node, SymbolContext context)
        {
            return node.Value switch
            {
                RecalcFormulaInfo fi => ResolvedObjectHelpers.RecalcFormulaInfo(fi),
                IExternalOptionSet optionSet => ResolvedObjectHelpers.OptionSet(optionSet, node.IRContext),
                _ => ResolvedObjectHelpers.ResolvedObjectError(node),
            };
        }
    }
}
