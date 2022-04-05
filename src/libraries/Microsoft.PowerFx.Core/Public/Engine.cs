// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Expose binding logic for Power Fx. 
    /// Derive from this to provide evaluation abilities. 
    /// </summary>
    public class Engine : IPowerFxEngine
    {
        /// <summary>
        /// Configuration symbols for this Power Fx engine.
        /// </summary>
        public PowerFxConfig Config { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// </summary>
        /// <param name="powerFxConfig"></param>
        public Engine(PowerFxConfig powerFxConfig)
        {
            powerFxConfig.Lock();
            Config = powerFxConfig;
        }

        /// <summary>
        /// List all functions (both builtin and custom) registered with this evaluator. 
        /// </summary>
        public IEnumerable<string> GetAllFunctionNames()
        {
            return Config.GetAllFunctionNames();
        }

        /// <summary>
        /// Create a resolver for use in binding. This is called from <see cref="Check(string, FormulaType)"/>.
        /// Base classes can override this is there are additional symbols not in the config.
        /// </summary>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        private protected virtual SimpleResolver CreateResolver()
        {
            return new SimpleResolver(Config);
        }

        /// <summary>
        /// Type check a formula without executing it. 
        /// </summary>
        /// <param name="expressionText"></param>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        public CheckResult Check(string expressionText, RecordType parameterType = null)
        {
            if (parameterType == null)
            {
                parameterType = new RecordType();
            }

            var formula = new Formula(expressionText);

            formula.EnsureParsed(TexlParser.Flags.None);

            // Ok to continue with binding even if there are parse errors. 
            // We can still use that for intellisense. 

            var resolver = CreateResolver();

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

                result.Expression = CreateEvaluator(result);
            }

            return result;
        }

        /// <summary>
        /// Derived class can override to provide evaluation ability. Called after binding to return an eval object. 
        /// </summary>
        /// <param name="result">results of binding.</param>
        /// <returns>An <see cref="IExpression"/> to apply to the result.</returns>
        protected virtual IExpression CreateEvaluator(CheckResult result)
        {
            // Nop. 
            return null;
        }

        /// <summary>
        /// Optional hook to customize intellisense. 
        /// </summary>
        /// <returns></returns>
        private protected virtual IIntellisense CreateIntellisense()
        {
            return IntellisenseProvider.GetIntellisense(Config.EnumStoreBuilder.Build());
        }

        /// <summary>
        /// Get intellisense from the formula.
        /// </summary>
        public IIntellisenseResult Suggest(string expression, RecordType parameterType, int cursorPosition)
        {
            var result = Check(expression, parameterType);
            var binding = result._binding;
            var formula = result._formula;

            var context = new IntellisenseContext(expression, cursorPosition);
            var intellisense = CreateIntellisense();
            var suggestions = intellisense.Suggest(context, binding, formula);

            return suggestions;
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

            var resolver = CreateResolver();
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
    }
}
