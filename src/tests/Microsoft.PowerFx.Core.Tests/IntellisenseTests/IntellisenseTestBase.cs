// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    /// <summary>
    /// Provides methods that may be used by Intellisense tests
    /// </summary>
    public class IntellisenseTestBase
    {
        /// <summary>
        /// This method receives a test case string, along with an optional context type that defines the valid
        /// names and types in the expression and invokes Intellisense.Suggest on it, and returns a the result
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="contextTypeString"></param>
        /// <returns></returns>
        internal IIntellisenseResult Suggest(string expression, PowerFxConfig powerFxConfig, string contextTypeString = null)
        {
            Assert.NotNull(expression);

            var cursorMatches = Regex.Matches(expression, @"\|");
            Assert.True(cursorMatches.Count == 1, "Invalid cursor.  Exactly one cursor must be specified.");
            var cursorPosition = cursorMatches.First().Index;

            expression = expression.Replace("|", string.Empty);

            RecordType contextType;
            if (contextTypeString != null)
            {
                DType.TryParse(contextTypeString, out var contextDType);
                contextType = FormulaType.Build(contextDType) as RecordType;

                Assert.True(contextType != null, "Context type must be a record type");
            }
            else
            {
                // We leave the context type as an empty record when none is provided
                contextType = new RecordType();
            }

            return Suggest(expression, contextType, cursorPosition, powerFxConfig);
        }

        internal IIntellisenseResult Suggest(string expression, FormulaType parameterType, int cursorPosition, PowerFxConfig powerFxConfig)
        {
            var formula = new Formula(expression);
            formula.EnsureParsed(TexlParser.Flags.None);

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                formula.ParseTree,
                new SimpleResolver(powerFxConfig.EnumStore.EnumSymbols),
                ruleScope: parameterType._type,
                useThisRecordForRuleScope: false
            );

            var context = new IntellisenseContext(expression, cursorPosition);
            var intellisense = IntellisenseProvider.GetIntellisense(powerFxConfig);
            var suggestions = intellisense.Suggest(context, binding, formula);

            if (suggestions.Exception != null)
            {
                throw suggestions.Exception;
            }

            return suggestions;
        }
    }
}
