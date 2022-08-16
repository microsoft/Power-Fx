// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    /// <summary>
    /// Provides methods that may be used by Intellisense tests.
    /// </summary>
    public class IntellisenseTestBase : PowerFxTest
    {
        /// <summary>
        /// This method receives a test case string, along with an optional context type that defines the valid
        /// names and types in the expression and invokes Intellisense.Suggest on it, and returns a the result.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="contextTypeString"></param>
        /// <returns></returns>
        internal IIntellisenseResult Suggest(string expression, PowerFxConfig config, string contextTypeString = null)
        {
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
                contextType = RecordType.Empty();
            }

            return Suggest(expression, config, contextType);
        }

        internal IIntellisenseResult Suggest(string expression, PowerFxConfig config, RecordType parameterType)
        {
            (var expression2, var cursorPosition) = Decode(expression);
            return Suggest(expression2, parameterType, cursorPosition, config);
        }

        // Tests use | to indicate cursor position within an expression string. 
        // Return the cursos position and string (without the |). 
        internal (string, int) Decode(string expression)
        {
            Assert.NotNull(expression);

            var cursorMatches = Regex.Matches(expression, @"\|");
            Assert.True(cursorMatches.Count == 1, "Invalid cursor.  Exactly one cursor must be specified.");
            var cursorPosition = cursorMatches.First().Index;

            expression = expression.Replace("|", string.Empty);

            return (expression, cursorPosition);
        }

        internal IIntellisenseResult Suggest(string expression, RecordType parameterType, int cursorPosition, PowerFxConfig config)
        {
            var engine = new Engine(config);

            var suggestions = engine.Suggest(expression, parameterType, cursorPosition);

            if (suggestions.Exception != null)
            {
                throw suggestions.Exception;
            }

            return suggestions;
        }
    }
}
