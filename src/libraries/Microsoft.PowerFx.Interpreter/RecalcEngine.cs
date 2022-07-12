// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds a set of Power Fx variables and formulas. Formulas are recalculated when their dependent variables change.
    /// </summary>
    public sealed class RecalcEngine : Engine, IScope, IPowerFxEngine
    {
        internal Dictionary<string, RecalcFormulaInfo> Formulas { get; } = new Dictionary<string, RecalcFormulaInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RecalcEngine"/> class.
        /// Create a new power fx engine. 
        /// </summary>
        public RecalcEngine()
            : this(new PowerFxConfig(null))
        {
        }

        public RecalcEngine(PowerFxConfig powerFxConfig)
            : base(AddInterpreterFunctions(powerFxConfig))
        {
        }

        // Add Builtin functions that aren't yet in the shared library. 
        private static PowerFxConfig AddInterpreterFunctions(PowerFxConfig powerFxConfig)
        {
            // Set to Interpreter's implemented list (not necessarily same as defaults)
            powerFxConfig.SetCoreFunctions(Library.FunctionList);

            // Add custom. 
            powerFxConfig.AddFunction(BuiltinFunctionsCore.DateTime);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Index_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.ParseJSON);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Table_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Text_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Value_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Boolean);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Boolean_T);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.BooleanN);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.BooleanN_T);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Boolean_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.CountRows_UO);

            return powerFxConfig;
        }

        /// <inheritdoc/>
        private protected override INameResolver CreateResolver(PowerFxConfig alternateConfig = null)
        {
            // The RecalcEngineResolver allows access to the values from UpdateValue. 
            var resolver = new RecalcEngineResolver(this, alternateConfig ?? Config);
            return resolver;
        }

        /// <inheritdoc/>
        protected override IExpression CreateEvaluator(CheckResult result)
        {
            return CreateEvaluatorDirect(result);
        }

        /// <summary>
        /// Create an evaluator over the existing binding. 
        /// </summary>
        /// <param name="result">A successful binding from a previous call to <see cref="Engine.Check(string, BaseRecordType, ParserOptions)"/>. </param>
        /// <returns></returns>
        public static IExpression CreateEvaluatorDirect(CheckResult result)
        {
            if (result._binding == null)
            {
                throw new InvalidOperationException($"Requires successful binding");
            }

            result.ThrowOnErrors();            

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(result._binding);
            return new ParsedExpression(irnode, ruleScopeSymbol);
        }

        // This handles lookups in the global scope. 
        FormulaValue IScope.Resolve(string name)
        {
            if (Formulas.TryGetValue(name, out var info))
            {
                return info.Value;
            }

            // Binder should have caught. 
            throw new InvalidOperationException($"Can't resolve '{name}'");
        }

        public void UpdateVariable(string name, double value)
        {
            UpdateVariable(name, new NumberValue(IRContext.NotInSource(FormulaType.Number), value));
        }

        /// <summary>
        /// Create or update a named variable to a value. 
        /// </summary>
        /// <param name="name">variable name. This can be used in other formulas.</param>
        /// <param name="value">constant value.</param>
        public void UpdateVariable(string name, FormulaValue value)
        {
            var x = value;

            if (Formulas.TryGetValue(name, out var fi))
            {
                // Type should match?
                if (fi._type != x.Type)
                {
                    throw new NotSupportedException($"Can't change '{name}''s type from {fi._type} to {x.Type}.");
                }

                fi.Value = x;

                // Be sure to preserve used-by set. 
            }
            else
            {
                Formulas[name] = new RecalcFormulaInfo { Value = x, _type = x.IRContext.ResultType };
            }

            // Could trigger recalcs?
            Recalc(name);
        }

        /// <summary>
        /// Evaluate an expression as text and return the result.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula.</param>
        /// <param name="options"></param>
        /// <returns>The formula's result.</returns>
        public FormulaValue Eval(string expressionText, RecordValue parameters = null, ParserOptions options = null)
        {
            return EvalAsync(expressionText, CancellationToken.None, parameters, options).Result;          
        }

        public async Task<FormulaValue> EvalAsync(string expressionText, CancellationToken cancel, RecordValue parameters = null, ParserOptions options = null)
        {
            if (parameters == null)
            {
                parameters = RecordValue.Empty();
            }

            var check = Check(expressionText, (BaseRecordType)parameters.IRContext.ResultType, options);
            check.ThrowOnErrors();

            var newValue = await check.Expression.EvalAsync(parameters, cancel);
            return newValue;
        }

        // Invoke onUpdate() each time this formula is changed, passing in the new value. 
        public void SetFormula(string name, string expr, Action<string, FormulaValue> onUpdate)
        {
            SetFormula(name, new FormulaWithParameters(expr), onUpdate);
        }

        /// <summary>
        /// Create a formula that will be recalculated when its dependent values change.
        /// </summary>
        /// <param name="name">name of formula. This can be used in other formulas.</param>
        /// <param name="expr">expression.</param>
        /// <param name="onUpdate">Callback to fire when this value is updated.</param>
        public void SetFormula(string name, FormulaWithParameters expr, Action<string, FormulaValue> onUpdate)
        {
            if (Formulas.ContainsKey(name))
            {
                throw new InvalidOperationException($"Can't change existing formula: {name}");
            }

            var check = Check(expr._expression, expr._schema);
            check.ThrowOnErrors();
            var binding = check._binding;

            // We can't have cycles because:
            // - formulas can only refer to already-defined values
            // - formulas can't be redefined.  
            var dependsOn = check.TopLevelIdentifiers;

            var type = FormulaType.Build(binding.ResultType);
            var info = new RecalcFormulaInfo
            {
                _dependsOn = dependsOn,
                _type = type,
                _binding = binding,
                _onUpdate = onUpdate
            };

            Formulas[name] = info;

            foreach (var x in dependsOn)
            {
                Formulas[x]._usedBy.Add(name);
            }

            Recalc(name);
        }

        // Trigger a recalc on name and anything that depends on it. 
        // Invoke on Update callbacks. 
        private void Recalc(string name)
        {
            var r = new RecalcEngineWorker(this);
            r.Recalc(name);
        }

        /// <summary>
        /// Delete formula that was previously created.
        /// </summary>
        /// <param name="name">Formula name.</param>
        public void DeleteFormula(string name)
        {
            if (Formulas.TryGetValue(name, out var fi))
            {
                if (fi._usedBy.Count == 0)
                {
                    foreach (var dependsOnName in fi._dependsOn)
                    {
                        if (Formulas.TryGetValue(dependsOnName, out var info))
                        {
                            info._usedBy.Remove(name);
                        }
                    }

                    Formulas.Remove(name);
                }
                else
                {
                    throw new InvalidOperationException($"Formula {name} cannot be deleted due to the following dependencies: {string.Join(", ", fi._usedBy)}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Formula {name} does not exist");
            }
        }

        /// <summary>
        /// Get the current value of a formula. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FormulaValue GetValue(string name)
        {
            var fi = Formulas[name];
            return fi.Value;
        }
    } // end class RecalcEngine
}
