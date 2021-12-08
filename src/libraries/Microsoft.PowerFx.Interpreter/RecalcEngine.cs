// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds a set of Power Fx variables and formulas. Formulas are recalculated when their dependent variables change.
    /// </summary>
    public class RecalcEngine : IScope
    {
        // User-provided functions 
        private Dictionary<string, TexlFunction> _extraFunctions = new Dictionary<string, TexlFunction>(StringComparer.OrdinalIgnoreCase);

        internal Dictionary<string, RecalcFormulaInfo> Formulas { get; } = new Dictionary<string, RecalcFormulaInfo>();

        PowerFxConfig _powerFxConfig;

        /// <summary>
        /// Create a new power fx engine. 
        /// </summary>
        /// <param name="powerFxConfig">Compiler customizations</param>
        public RecalcEngine(PowerFxConfig powerFxConfig = null)
        {
            _powerFxConfig = powerFxConfig ?? new PowerFxConfig();
        }

        /// <summary>
        /// Add a custom function. 
        /// </summary>
        /// <param name="function"></param>
        public void AddFunction(ReflectionFunction function)
        {
            var texl = function.GetTexlFunction();
            _extraFunctions[texl.Name] = texl; // throw if already exists. 
        }

        /// <summary>
        /// List all functions (both builtin and custom) registered with this evaluator. 
        /// </summary>
        public IEnumerable<string> GetAllFunctionNames()
        {
            foreach (var kv in _extraFunctions) { yield return kv.Key; }
            foreach (var func in Functions.Library.FunctionList) { yield return func.Name; }
        }

        // This handles lookups in the global scope. 
        FormulaValue IScope.Resolve(string name)
        {
            if (Formulas.TryGetValue(name, out var info))
            {
                return info._value;
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
                fi._value = x;

                // Be sure to preserve used-by set. 
            }
            else
            {
                Formulas[name] = new RecalcFormulaInfo { _value = x, _type = x.IRContext.ResultType };
            }

            // Could trigger recalcs?
            Recalc(name);
        }

        /// <summary>
        /// Type check a formula without executing it. 
        /// </summary>
        /// <param name="expressionText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public CheckResult Check(string expressionText, FormulaType parameterType = null)
        {
            return CheckInternal(expressionText, parameterType, intellisense: false);
        }

        private CheckResult CheckInternal(string expressionText, FormulaType parameterType = null, bool intellisense = false)
        {
            if (parameterType == null)
            {
                parameterType = new RecordType();
            }

            var formula = new Formula(expressionText);

            formula.EnsureParsed(TexlParser.Flags.None);

            // Ok to continue with binding even if there are parse errors. 
            // We can still use that for intellisense. 

            var extraFunctions = _extraFunctions.Values.ToArray();

            var resolver = new RecalcEngineResolver(this, (RecordType)parameterType, _powerFxConfig.EnumStore.EnumSymbols, extraFunctions);

            // $$$ - intellisense only works with ruleScope.
            // So if running for intellisense, pass the parameters in ruleScope. 
            // But if running for eval, let the resolver handle the parameters. 
            // Resolver is only invoked if not in RuleScope. 

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                formula.ParseTree,
                resolver,
                ruleScope: intellisense ? parameterType._type : null,
                useThisRecordForRuleScope: false
            );

            var errors = (formula.HasParseErrors) ? formula.GetParseErrors() : binding.ErrorContainer.GetErrors();

            var result = new CheckResult
            {
                _binding = binding,
                _formula = formula,
            };

            if (errors != null && errors.Any())
            {
                result.SetErrors(errors.ToArray());
            }
            else
            {
                result.TopLevelIdentifiers = DependencyFinder.FindDependencies(binding.Top, binding);
                // TODO: Fix FormulaType.Build to not throw exceptions for Enum types then remove this check
                if (binding.ResultType.Kind != DKind.Enum)
                    result.ReturnType = FormulaType.Build(binding.ResultType);
            }

            return result;
        }

        /// <summary>
        /// Evaluate an expression as text and return the result.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula</param>
        /// <param name="parameters">parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula.</param>
        /// <returns>The formula's result</returns>
        public FormulaValue Eval(string expressionText, RecordValue parameters = null)
        {
            if (parameters == null)
            {
                parameters = RecordValue.Empty();
            }
            var check = Check(expressionText, parameters.IRContext.ResultType);
            check.ThrowOnErrors();

            var binding = check._binding;


            (IntermediateNode irnode, ScopeSymbol ruleScopeSymbol) = IRTranslator.Translate(binding);

            var ev2 = new EvalVisitor(_powerFxConfig.CultureInfo);
            FormulaValue newValue = irnode.Accept(ev2, SymbolContext.New().WithGlobals(parameters));

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
            HashSet<string> dependsOn = check.TopLevelIdentifiers;

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

        /// <summary>
        /// Get intellisense from the formula.
        /// </summary>
        public IIntellisenseResult Suggest(string expression, FormulaType parameterType, int cursorPosition)
        {
            var result = this.CheckInternal(expression, parameterType, intellisense: true);
            var binding = result._binding;
            var formula = result._formula;

            var context = new IntellisenseContext(expression, cursorPosition);
            var intellisense = IntellisenseProvider.GetIntellisense(_powerFxConfig);
            var suggestions = intellisense.Suggest(context, binding, formula);

            return suggestions;
        }

        // Trigger a recalc on name and anything that depends on it. 
        // Invoke on Update callbacks. 
        private void Recalc(string name)
        {
            var r = new RecalcEngineWorker(this);
            r.Recalc(name);
        }

        public void DeleteFormula(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the current value of a formula. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FormulaValue GetValue(string name)
        {
            var fi = Formulas[name];
            return fi._value;
        }
    } // end class RecalcEngine
}
