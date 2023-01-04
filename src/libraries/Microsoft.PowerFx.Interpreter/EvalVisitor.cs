// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Engine;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx
{
    // This used ValueTask for async, https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/ 
    // Perf comparison of Task vs. ValueTask: https://ladeak.wordpress.com/2019/03/09/valuetask-vs-task 
    // Use Task for public methods, but ValueTask for internal methods that we expect to be mostly sync. 
    internal class EvalVisitor : IRNodeVisitor<ValueTask<FormulaValue>, EvalVisitorContext>
    {
        private readonly ReadOnlySymbolValues _symbolValues;

        private readonly CancellationToken _cancellationToken;

        internal CancellationToken CancellationToken => _cancellationToken;

        private readonly IServiceProvider _services;

        public IServiceProvider FunctionServices => _services;

        public CultureInfo CultureInfo { get; private set; }

        public EvalVisitor(IRuntimeConfig config, CancellationToken cancellationToken)
        {
            _symbolValues = config.Values; // may be null 
            _cancellationToken = cancellationToken;

            _services = config.ServiceProvider ?? new BasicServiceProvider();

            CultureInfo = GetService<CultureInfo>();
        }

        /// <summary>
        /// Get a service from the <see cref="ReadOnlySymbolValues"/>. Returns null if not present.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return (T)_services.GetService(typeof(T));            
        }

        public bool TryGetService<T>(out T result)
        {
            result = GetService<T>();
            return result != null;
        }

        // Check this cooperatively - especially in any loop. 
        public void CheckCancel()
        {
            // Throws OperationCanceledException exception
            _cancellationToken.ThrowIfCancellationRequested();
        }

        // Helper to eval an arg that might be a lambda.
        internal async ValueTask<DValue<T>> EvalArgAsync<T>(FormulaValue arg, EvalVisitorContext context, IRContext irContext)
            where T : ValidFormulaValue
        {
            if (arg is LambdaFormulaValue lambda)
            {
                arg = await lambda.EvalInRowScopeAsync(context);
            }

            return arg switch
            {
                T t => DValue<T>.Of(t),
                BlankValue b => DValue<T>.Of(b),
                ErrorValue e => DValue<T>.Of(e),
                _ => DValue<T>.Of(CommonErrors.RuntimeTypeMismatch(irContext))
            };
        }

        public override async ValueTask<FormulaValue> Visit(TextLiteralNode node, EvalVisitorContext context)
        {
            return new StringValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(NumberLiteralNode node, EvalVisitorContext context)
        {
            return new NumberValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(BooleanLiteralNode node, EvalVisitorContext context)
        {
            return new BooleanValue(node.IRContext, node.LiteralValue);
        }

        public override async ValueTask<FormulaValue> Visit(RecordNode node, EvalVisitorContext context)
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

        public override async ValueTask<FormulaValue> Visit(LazyEvalNode node, EvalVisitorContext context)
        {
            var val = await node.Child.Accept(this, context);
            return val;
        }

        // Handle the Set() function -
        // Set is unique because it has an l-value for the first arg. 
        // Async params can't have out-params. 
        // Return null if not handled. Else non-null if handled.
        private async Task<FormulaValue> TryHandleSet(CallNode node, EvalVisitorContext context)
        {
            // Special case Set() calls because they take an LValue. 
            if (node.Function.GetType() != typeof(RecalcEngineSetFunction))
            {
                return null;
            }

            var arg0 = node.Args[0];
            var arg1 = node.Args[1];

            var newValue = await arg1.Accept(this, context);

            // Binder has already ensured this is a first name node as well as mutable symbol. 
            if (arg0 is ResolvedObjectNode obj)
            {
                if (obj.Value is ISymbolSlot sym)
                {
                    if (_symbolValues != null)
                    {
                        _symbolValues.Set(sym, newValue);
                        return FormulaValue.New(true);
                    }

                    // This may happen if the runtime symbols are missing a value and we failed to update. 
                }    
            }

            // Fail?
            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        // Handle invoke SetProperty(source.Prop, newValue)
        // Invoke as: SetProperty(source, "Prop", newValue)
        private async Task<FormulaValue> TryHandleSetProperty(CallNode node, EvalVisitorContext context)
        {
            if (node.Function is not CustomSetPropertyFunction setPropFunc)
            {
                return null;
            }

            var arg0 = node.Args[0];
            var arg1 = node.Args[1];

            if (arg0 is not RecordFieldAccessNode r)
            {
                return null;
            }

            var source = await r.From.Accept(this, context);
            var fieldName = r.Field.Value;
            var newValue = await arg1.Accept(this, context);

            var args = new FormulaValue[] { source, FormulaValue.New(fieldName), newValue };
            var result = await setPropFunc.InvokeAsync(args, _cancellationToken);

            return result;
        }

        public override async ValueTask<FormulaValue> Visit(CallNode node, EvalVisitorContext context)
        {
            CheckCancel();

            var setResult = await TryHandleSet(node, context.IncrementStackDepthCounter());
            if (setResult != null)
            {
                return setResult;
            }

            var setPropResult = await TryHandleSetProperty(node, context.IncrementStackDepthCounter());
            if (setPropResult != null)
            {
                return setPropResult;
            }

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
                    args[i] = await child.Accept(this, context.IncrementStackDepthCounter());
                }
                else
                {
                    args[i] = new LambdaFormulaValue(node.IRContext, child, this, context);
                }
            }

            var childContext = context.SymbolContext.WithScope(node.Scope);

            if (func is IAsyncTexlFunction asyncFunc)
            {
                var result = await asyncFunc.InvokeAsync(args, _cancellationToken);
                return result;
            }
            else if (func is UserDefinedTexlFunction udtf)
            {
                // $$$ Should add _runtimeConfig
                var result = await udtf.InvokeAsync(args, _cancellationToken, context.StackDepthCounter.Increment());
                return result;
            }
            else if (func is CustomTexlFunction customTexlFunc)
            {
                var result = await customTexlFunc.InvokeAsync(FunctionServices, args, _cancellationToken);
                return result;
            }
            else
            {
                if (FunctionImplementations.TryGetValue(func, out var ptr))
                {
                    var result = await ptr(this, context.IncrementStackDepthCounter(childContext), node.IRContext, args);

                    if (IfFunction.CanCheckIfReturn(func))
                    {
                        Contract.Assert(result.IRContext.ResultType == node.IRContext.ResultType || result is ErrorValue || result.IRContext.ResultType is BlankType);
                    }

                    return result;
                }

                return CommonErrors.NotYetImplementedError(node.IRContext, $"Missing func: {func.Name}");
            }
        }

        public override async ValueTask<FormulaValue> Visit(BinaryOpNode node, EvalVisitorContext context)
        {
            var arg1 = await node.Left.Accept(this, context);
            var arg2 = await node.Right.Accept(this, context);
            var args = new FormulaValue[] { arg1, arg2 };
            return await VisitBinaryOpNode(node, context, args);
        }

        private ValueTask<FormulaValue> VisitBinaryOpNode(BinaryOpNode node, EvalVisitorContext context, FormulaValue[] args)
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
                case BinaryOpKind.EqNull:
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
                case BinaryOpKind.NeqNull:
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
                case BinaryOpKind.AddTimeAndNumber:
                    return OperatorAddTimeAndNumber(this, context, node.IRContext, args);
                case BinaryOpKind.AddNumberAndTime:
                    return OperatorAddTimeAndNumber(this, context, node.IRContext, new[] { args[1], args[0] });
                case BinaryOpKind.AddTimeAndTime:
                    return OperatorAddTimeAndTime(this, context, node.IRContext, args);
                case BinaryOpKind.DateDifference:
                    return OperatorDateDifference(this, context, node.IRContext, args);
                case BinaryOpKind.TimeDifference:
                    return OperatorTimeDifference(this, context, node.IRContext, args);
                case BinaryOpKind.SubtractDateAndTime:
                    return OperatorSubtractDateAndTime(this, context, node.IRContext, args);
                case BinaryOpKind.SubtractNumberAndDate:
                    return OperatorSubtractNumberAndDate(this, context, node.IRContext, args);
                case BinaryOpKind.SubtractNumberAndTime:
                    return OperatorSubtractNumberAndTime(this, context, node.IRContext, args);
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
                    return new ValueTask<FormulaValue>(OperatorDynamicGetField(node, args));

                default:
                    return new ValueTask<FormulaValue>(CommonErrors.UnreachableCodeError(node.IRContext));
            }
        }

        private static FormulaValue OperatorDynamicGetField(BinaryOpNode node, FormulaValue[] args)
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
                        Kind = ErrorKind.InvalidArgument
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

        public override async ValueTask<FormulaValue> Visit(UnaryOpNode node, EvalVisitorContext context)
        {
            var arg1 = await node.Child.Accept(this, context);
            var args = new FormulaValue[] { arg1 };

            if (UnaryOps.TryGetValue(node.Op, out var unaryOp))
            {
                return await unaryOp(this, context, node.IRContext, args);
            }

            return CommonErrors.NotYetImplementedError(node.IRContext, $"Unary op {node.Op}");
        }

        public override async ValueTask<FormulaValue> Visit(AggregateCoercionNode node, EvalVisitorContext context)
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
                        var scopeContext = context.SymbolContext.WithScope(node.Scope);
                        foreach (var coercion in node.FieldCoercions)
                        {
                            CheckCancel();

                            var record = row.Value;
                            var newScope = scopeContext.WithScopeValues(record);

                            var newValue = await coercion.Value.Accept(this, context.NewScope(newScope));
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

        public override async ValueTask<FormulaValue> Visit(ScopeAccessNode node, EvalVisitorContext context)
        {
            if (node.Value is ScopeAccessSymbol s1)
            {
                var scope = s1.Parent;

                var val = context.SymbolContext.GetScopeVar(scope, s1.Name);
                return val;
            }

            // Binds to whole scope
            if (node.Value is ScopeSymbol s2)
            {
                var r = context.SymbolContext.ScopeValues[s2.Id];
                if (r is RecordScope r2)
                {
                    return r2._context;
                }
                else if (r is UntypedObjectThisRecordScope r3)
                {
                    return r3._thisRecord;
                }
            }

            return CommonErrors.UnreachableCodeError(node.IRContext);
        }

        public override async ValueTask<FormulaValue> Visit(RecordFieldAccessNode node, EvalVisitorContext context)
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
            var val = await record.GetFieldAsync(node.IRContext.ResultType, node.Field.Value, _cancellationToken);

            return val;
        }

        public override async ValueTask<FormulaValue> Visit(SingleColumnTableAccessNode node, EvalVisitorContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Single column table access");
        }

        public override async ValueTask<FormulaValue> Visit(ErrorNode node, EvalVisitorContext context)
        {
            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = node.ErrorHint,
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.AnalysisError
            });
        }

        public override async ValueTask<FormulaValue> Visit(ColorLiteralNode node, EvalVisitorContext context)
        {
            return CommonErrors.NotYetImplementedError(node.IRContext, "Color literal");
        }

        public override async ValueTask<FormulaValue> Visit(ChainingNode node, EvalVisitorContext context)
        {
            CheckCancel();

            if (!node.Nodes.Any())
            {
                return CommonErrors.InvalidChain(node.IRContext, node.ToString());
            }

            FormulaValue fv = null;
            var errors = new List<ExpressionError>();

            foreach (var iNode in node.Nodes)
            {
                CheckCancel();

                fv = await iNode.Accept(this, context);

                if (fv is ErrorValue ev)
                {
                    errors.AddRange(ev.Errors);
                }
            }

            return errors.Any() ? new ErrorValue(node.IRContext, errors) : fv;
        }

        public override async ValueTask<FormulaValue> Visit(ResolvedObjectNode node, EvalVisitorContext context)
        {
            return node.Value switch
            {
                NameSymbol name => GetVariableOrFail(node, name),
                FormulaValue fi => fi,
                IExternalOptionSet optionSet => ResolvedObjectHelpers.OptionSet(optionSet, node.IRContext),
                _ => ResolvedObjectHelpers.ResolvedObjectError(node),
            };
        }

        private FormulaValue GetVariableOrFail(ResolvedObjectNode node, ISymbolSlot slot)
        {
            if (_symbolValues != null)                
            {
                var value = _symbolValues.Get(slot);
                if (value != null)
                {
                    return value;
                }
            }

            return ResolvedObjectHelpers.ResolvedObjectError(node);
        }
    }
}
