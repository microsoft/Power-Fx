// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Evaluate an expression that was successfully bound.  
    /// Get this from <see cref="CheckResult"/>.GetEvaluator() extension method.
    /// </summary>
    public interface IExpressionEvaluator
    {
        public Task<FormulaValue> EvalAsync(CancellationToken cancellationToken, ReadOnlySymbolValues runtimeConfig = null);
    }

    // Extensions for adding evaluation methods. 
    // CheckResult lives in Core assembly. But evaluation is in this assembly (not core). 
    // Creating an evaluator uses internal state. 
    public static class CheckResultExtensions
    {
        public static FormulaValue Eval(this IExpressionEvaluator expr, ReadOnlySymbolValues runtimeConfig = null)
        {
            return expr.EvalAsync(CancellationToken.None, runtimeConfig).Result;
        }

        public static FormulaValue Eval(this IExpressionEvaluator expr, RecordValue parameters)
        {
            return expr.EvalAsync(CancellationToken.None, parameters).Result;
        }

        public static async Task<FormulaValue> EvalAsync(this IExpressionEvaluator expr, CancellationToken cancellationToken, RecordValue parameters)
        {
            var runtimeConfig = SymbolValues.NewFromRecord(parameters);
            return await expr.EvalAsync(cancellationToken, runtimeConfig);
        }

        /// <summary>
        /// Get an evaluator for this expression which can be executed many times. 
        /// </summary>
        /// <param name="result">Result of a successful binding.</param>
        /// <returns></returns>
        public static IExpressionEvaluator GetEvaluator(this CheckResult result)
        {
            var stackMarker = new StackDepthCounter(PowerFxConfig.DefaultMaxCallDepth);
            return GetEvaluator(result, stackMarker);
        }

        internal static IExpressionEvaluator GetEvaluator(this CheckResult result, StackDepthCounter stackMarker)
        {
            if (result._binding == null)
            {
                throw new InvalidOperationException($"Requires successful binding");
            }

            result.ThrowOnErrors();

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(result._binding);
            return new ParsedExpression(irnode, ruleScopeSymbol, stackMarker, result.CultureInfo);
        }
    }

    internal class ParsedExpression : IExpression, IExpressionEvaluator
    {
        internal IntermediateNode _irnode;
        private readonly ScopeSymbol _topScopeSymbol;
        private readonly CultureInfo _cultureInfo;
        private readonly StackDepthCounter _stackMarker;

        internal ParsedExpression(IntermediateNode irnode, ScopeSymbol topScope, StackDepthCounter stackMarker, CultureInfo cultureInfo)
        {
            _irnode = irnode;
            _topScopeSymbol = topScope;
            _stackMarker = stackMarker;
            _cultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
        }

        public async Task<FormulaValue> EvalAsync(RecordValue parameters, CancellationToken cancellationToken)
        {
            var useRowScope = _topScopeSymbol.AccessedFields.Count > 0;
            ReadOnlySymbolValues runtimeConfig = null;

            // For backwards compat - if a caller with internals access created a IR that binds to 
            // rowscope directly, then apply parameters to row scope. 
            if (!useRowScope)
            {
                runtimeConfig = SymbolValues.NewFromRecord(parameters);
                parameters = RecordValue.Empty();
            }

            var evalVisitor = new EvalVisitor(_cultureInfo, cancellationToken, runtimeConfig);
            try
            {
                var newValue = await _irnode.Accept(evalVisitor, new EvalVisitorContext(SymbolContext.NewTopScope(_topScopeSymbol, parameters), _stackMarker));
                return newValue;
            }
            catch (MaxCallDepthException maxCallDepthException)
            {
                return maxCallDepthException.ToErrorValue(_irnode.IRContext);
            }
        }

        public async Task<FormulaValue> EvalAsync(CancellationToken cancellationToken, ReadOnlySymbolValues runtimeConfig = null)
        {
            var culture = runtimeConfig?.GetService<CultureInfo>() ?? _cultureInfo;
            var evalVisitor = new EvalVisitor(culture, cancellationToken, runtimeConfig);

            try
            {
                var newValue = await _irnode.Accept(evalVisitor, new EvalVisitorContext(SymbolContext.New(), _stackMarker));
                return newValue;
            }
            catch (MaxCallDepthException maxCallDepthException)
            {
                return maxCallDepthException.ToErrorValue(_irnode.IRContext);
            }
        }

        internal async Task<FormulaValue> EvalAsyncInternal(RecordValue parameters, CancellationToken cancel, StackDepthCounter stackMarker)
        {
            var runtimeConfig = SymbolValues.NewFromRecord(parameters);
            parameters = RecordValue.Empty();

            // We don't catch the max call depth exception here becuase someone could swallow the error with an "IfError" check.
            // Instead we only catch at the top of parsed expression, which is the above function.
            var ev2 = new EvalVisitor(_cultureInfo, cancel, runtimeConfig);
            var newValue = await _irnode.Accept(ev2, new EvalVisitorContext(SymbolContext.NewTopScope(_topScopeSymbol, parameters), stackMarker));
            return newValue;
        }
    }
}
