// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
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
        public Task<FormulaValue> EvalAsync(CancellationToken cancellationToken, IRuntimeConfig runtimeConfig = null);
    }

    // Extensions for adding evaluation methods. 
    // CheckResult lives in Core assembly. But evaluation is in this assembly (not core). 
    // Creating an evaluator uses internal state. 
    public static class CheckResultExtensions
    {
        public static FormulaValue Eval(this IExpressionEvaluator expr, ReadOnlySymbolValues runtimeConfig)
        {
            return expr.EvalAsync(CancellationToken.None, new RuntimeConfig(runtimeConfig)).Result;
        }

        public static FormulaValue Eval(this IExpressionEvaluator expr, IRuntimeConfig runtimeConfig = null)
        {
            return expr.EvalAsync(CancellationToken.None, runtimeConfig).Result;
        }

        public static FormulaValue Eval(this IExpressionEvaluator expr, RecordValue parameters)
        {
            return expr.EvalAsync(CancellationToken.None, parameters).Result;
        }

        public static async Task<FormulaValue> EvalAsync(this IExpressionEvaluator expr, CancellationToken cancellationToken, RecordValue parameters)
        {
            // If we eval with a RecordValue, we must have called Check with a RecordType. 
            var parameterType = ((ParsedExpression)expr)._parameterSymbolTable;
            var runtimeConfig = SymbolValues.NewFromRecord(parameterType, parameters);
            return await expr.EvalAsync(cancellationToken, new RuntimeConfig(runtimeConfig)).ConfigureAwait(false);
        }

        public static async Task<FormulaValue> EvalAsync(this IExpressionEvaluator expr, CancellationToken cancellationToken, ReadOnlySymbolValues symbolValues)
        {
            var runtimeConfig = new RuntimeConfig(symbolValues);
            return await expr.EvalAsync(cancellationToken, runtimeConfig).ConfigureAwait(false);
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
            ReadOnlySymbolValues globals = null;

            if (result.Engine is RecalcEngine recalcEngine)
            {
                // Pull global values from the engine. 
                globals = recalcEngine._symbolValues;
            }

            var irResult = result.ApplyIR();
            result.ThrowOnErrors();

            var expr = new ParsedExpression(irResult.TopNode, irResult.RuleScopeSymbol, stackMarker, result.ParserCultureInfo)
            {
                _globals = globals,
                _allSymbols = result.Symbols,
                _parameterSymbolTable = result.Parameters,
                _additionalFunctions = result.Engine.Config.AdditionalFunctions,
                _features = result.Engine.Config.Features
            };

            return expr;
        }
    }

    internal class ParsedExpression : IExpressionEvaluator
    {
        internal IntermediateNode _irnode;
        private readonly ScopeSymbol _topScopeSymbol;
        private readonly CultureInfo _cultureInfo;
        private readonly StackDepthCounter _stackMarker;

        internal ReadOnlySymbolValues _globals;
        internal ReadOnlySymbolTable _allSymbols;
        internal ReadOnlySymbolTable _parameterSymbolTable;
        internal IReadOnlyDictionary<TexlFunction, IAsyncTexlFunction> _additionalFunctions;
        internal Features _features;

        internal ParsedExpression(IntermediateNode irnode, ScopeSymbol topScope, StackDepthCounter stackMarker, CultureInfo cultureInfo = null)
        {
            _irnode = irnode;
            _topScopeSymbol = topScope;
            _stackMarker = stackMarker;

            // $$$ can't use current culture
            _cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        public async Task<FormulaValue> EvalAsync(CancellationToken cancellationToken, IRuntimeConfig runtimeConfig = null)
        {
            ReadOnlySymbolValues symbolValues = ComposedReadOnlySymbolValues.New(false, _allSymbols, runtimeConfig?.Values, _globals);
            BasicServiceProvider innerServices = new BasicServiceProvider();
            bool hasInnerServices = false;

            if (_cultureInfo != null)
            {
                innerServices.AddService(_cultureInfo);
                hasInnerServices = true;
            }

            if (_additionalFunctions != null && _additionalFunctions.Any())
            {
                innerServices.AddService(_additionalFunctions);
                hasInnerServices = true;
            }

            if (_features != null)
            {
                innerServices.AddService(_features);
                hasInnerServices = true;
            }

            RuntimeConfig runtimeConfig2 = new RuntimeConfig
            {
                Values = symbolValues,
                ServiceProvider = new BasicServiceProvider(runtimeConfig?.ServiceProvider, hasInnerServices ? innerServices : null)
            };

            var evalVisitor = new EvalVisitor(runtimeConfig2, cancellationToken);

            try
            {
                var newValue = await _irnode.Accept(evalVisitor, new EvalVisitorContext(SymbolContext.New(), _stackMarker)).ConfigureAwait(false);
                return newValue;
            }
            catch (CustomFunctionErrorException customError)
            {
                var error = new ErrorValue(_irnode.IRContext, customError.ExpressionError);
                return error;
            }
            catch (MaxCallDepthException maxCallDepthException)
            {
                return maxCallDepthException.ToErrorValue(_irnode.IRContext);
            }
        }

        internal async Task<FormulaValue> EvalAsyncInternal(RecordValue parameters, CancellationToken cancel, StackDepthCounter stackMarker)
        {
            var symbolValues = SymbolValues.NewFromRecord(_parameterSymbolTable, parameters);
            parameters = RecordValue.Empty();

            var runtimeConfig2 = new RuntimeConfig
            {
                Values = symbolValues
            };

            if (_cultureInfo != null)
            {
                runtimeConfig2.SetCulture(_cultureInfo);
            }

            // We don't catch the max call depth exception here becuase someone could swallow the error with an "IfError" check.
            // Instead we only catch at the top of parsed expression, which is the above function.
            var ev2 = new EvalVisitor(runtimeConfig2, cancel);
            var newValue = await _irnode.Accept(ev2, new EvalVisitorContext(SymbolContext.NewTopScope(_topScopeSymbol, parameters), stackMarker)).ConfigureAwait(false);
            return newValue;
        }
    }
}
