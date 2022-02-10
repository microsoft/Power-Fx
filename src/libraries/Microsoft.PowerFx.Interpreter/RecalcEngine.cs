// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Holds a set of Power Fx variables and formulas. Formulas are recalculated when their dependent variables change.
    /// </summary>
    public class RecalcEngine : IScope, IPowerFxEngine
    {
        internal Dictionary<string, RecalcFormulaInfo> Formulas { get; } = new Dictionary<string, RecalcFormulaInfo>();

        private readonly PowerFxConfig _powerFxConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecalcEngine"/> class.
        /// Create a new power fx engine. 
        /// </summary>
        /// <param name="powerFxConfig">Compiler customizations.</param>
        public RecalcEngine(PowerFxConfig powerFxConfig = null)
        {
            powerFxConfig = powerFxConfig ?? new PowerFxConfig(null, null);
            AddInterpreterFunctions(powerFxConfig);
            powerFxConfig.Lock();
            _powerFxConfig = powerFxConfig;
        }

        // Add Builtin functions that aren't yet in the shared library. 
        private void AddInterpreterFunctions(PowerFxConfig powerFxConfig)
        {
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Index_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.ParseJson);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Table_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Text_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Value_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Boolean);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.Boolean_UO);
            powerFxConfig.AddFunction(BuiltinFunctionsCore.StringInterpolation);
        }

        /// <summary>
        /// List all functions (both builtin and custom) registered with this evaluator. 
        /// </summary>
        public IEnumerable<string> GetAllFunctionNames()
        {
            foreach (var kv in _powerFxConfig.ExtraFunctions)
            {
                yield return kv.Value.Name;
            }

            foreach (var func in Functions.Library.FunctionList)
            {
                yield return func.Name;
            }
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

            var resolver = new RecalcEngineResolver(this, _powerFxConfig, (RecordType)parameterType);

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                formula.ParseTree,
                resolver,
                ruleScope: parameterType._type,
                useThisRecordForRuleScope: false);

            var errors = formula.HasParseErrors ? formula.GetParseErrors() : binding.ErrorContainer.GetErrors();

            var result = new CheckResult
            {
                _binding = binding,
                _formula = formula,
            };

            if (errors != null && errors.Any())
            {
                result.SetErrors(errors.ToArray());
                result.Expression = null;
            }
            else
            {
                result.TopLevelIdentifiers = DependencyFinder.FindDependencies(binding.Top, binding);

                // TODO: Fix FormulaType.Build to not throw exceptions for Enum types then remove this check
                if (binding.ResultType.Kind != DKind.Enum)
                {
                    result.ReturnType = FormulaType.Build(binding.ResultType);
                }

                (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(result._binding);
                result.Expression = new ParsedExpression(irnode, ruleScopeSymbol);
            }

            return result;
        }

        /// <summary>
        /// Evaluate an expression as text and return the result.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula.</param>
        /// <returns>The formula's result.</returns>
        public FormulaValue Eval(string expressionText, RecordValue parameters = null)
        {
            if (parameters == null)
            {
                parameters = RecordValue.Empty();
            }

            var check = Check(expressionText, parameters.IRContext.ResultType);
            check.ThrowOnErrors();

            var newValue = check.Expression.Eval(parameters);
            return newValue;
        }

        /// <summary>
        /// Convert references in an expression to the invariant form.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">Type of parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula. If DisplayNames are used, make sure to have that mapping
        /// as part of the RecordType.
        /// <returns>The formula, with all identifiers converted to invariant form</returns>
        public string GetInvariantExpression(string expressionText, RecordType parameters)
        {
            return ConvertExpression(expressionText, parameters, toDisplayNames: false);
        }

        /// <summary>
        /// Convert references in an expression to the display form.
        /// </summary>
        /// <param name="expressionText">textual representation of the formula.</param>
        /// <param name="parameters">Type of parameters for formula. The fields in the parameter record can 
        /// be acecssed as top-level identifiers in the formula. If DisplayNames are used, make sure to have that mapping
        /// as part of the RecordType.
        /// <returns>The formula, with all identifiers converted to display form</returns>
        public string GetDisplayExpression(string expressionText, RecordType parameters)
        {
            return ConvertExpression(expressionText, parameters, toDisplayNames: true);
        }

        private string ConvertExpression(string expressionText, RecordType parameters, bool toDisplayNames)
        {
            var formula = new Formula(expressionText);
            formula.EnsureParsed(TexlParser.Flags.None);

            var resolver = new RecalcEngineResolver(this, _powerFxConfig, parameters);
            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                null,
                new Core.Entities.QueryOptions.DataSourceToQueryOptionsMap(),
                formula.ParseTree,
                resolver,
                ruleScope: parameters._type,
                useThisRecordForRuleScope: false,
                updateDisplayNames: toDisplayNames,
                forceUpdateDisplayNames: toDisplayNames);

            Dictionary<Span, string> worklist = new ();
            foreach (var token in binding.NodesToReplace)
            {
                worklist.Add(token.Key.Span, TexlLexer.EscapeName(token.Value));
            }

            return Span.ReplaceSpans(expressionText, worklist);
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

        /// <summary>
        /// Get intellisense from the formula.
        /// </summary>
        public IIntellisenseResult Suggest(string expression, FormulaType parameterType, int cursorPosition)
        {
            var result = CheckInternal(expression, parameterType, intellisense: true);
            var binding = result._binding;
            var formula = result._formula;

            var context = new IntellisenseContext(expression, cursorPosition);
            var intellisense = IntellisenseProvider.GetIntellisense(_powerFxConfig.EnumStore);
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
