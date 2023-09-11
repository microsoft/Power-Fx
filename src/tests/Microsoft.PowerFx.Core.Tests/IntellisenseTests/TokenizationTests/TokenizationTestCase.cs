// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    internal class TokenizationTestCase
    {
        public string Expression { get; set; }

        public IEnumerable<ExpectedToken> ExpectedTokens { get; set; }

        public ParserOptions Options { get; set; } = null;

        public IReadOnlyCollection<TokenType> TokenTypesToSkip = null;

        public TokenizationTestCase(string expr, params ExpectedToken[] expectedTokens)
        {
            Expression = expr;
            ExpectedTokens = expectedTokens;
        }

        public static TokenizationTestCase Create(string expr, params ExpectedToken[] expectedTokens)
        {
            BuildStartIdxs(expectedTokens);

            // Ignore placeholder tokens that might have been inserted to correctly compute relative start indexes
            expectedTokens = expectedTokens.Where(tok => !tok.IsIgnoredPlaceholderToken).ToArray();
            return new TokenizationTestCase(expr, expectedTokens);
        }

        public static TokenizationTestCase Create(string expr, ParserOptions options, params ExpectedToken[] expectedTokens)
        {
            var testCase = Create(expr, expectedTokens);
            testCase.Options = options;
            return testCase;
        }

        public static TokenizationTestCase Create(string expr, ParserOptions options, IReadOnlyCollection<TokenType> tokenTypesToSkip = null, params ExpectedToken[] expectedTokens)
        {
            var testCase = Create(expr, expectedTokens);
            testCase.Options = options;
            testCase.TokenTypesToSkip = tokenTypesToSkip;
            return testCase;
        }

        public static TokenizationTestCase Create(string expr, IReadOnlyCollection<TokenType> tokenTypesToSkip = null, params ExpectedToken[] expectedTokens)
        {
            var testCase = Create(expr, expectedTokens);
            testCase.TokenTypesToSkip = tokenTypesToSkip;
            return testCase;
        }

        private static void BuildStartIdxs(ExpectedToken[] expectedTokens)
        {
            int prevTokenEndIndex = 0;
            foreach (var token in expectedTokens)
            {
                if (token.StartIndex == ExpectedToken.SpecialStartIdx)
                {
                    token.StartIndex = prevTokenEndIndex;
                }

                prevTokenEndIndex = token.EndIndex;
            }
        }

        public override string ToString()
        {
            var expectedTokens = JsonSerializer.Serialize(ExpectedTokens.Select(token => token.ToString()));
            var tokenTypesToSkip = JsonSerializer.Serialize(TokenTypesToSkip != null ? TokenTypesToSkip.Select(type => type.ToString()) : new List<string>());
            return $"\nExpression: {Expression}\nExpectedTokens: {expectedTokens}\nTokenTypesToSkip={tokenTypesToSkip}";
        }

        public static IEnumerable<object[]> TestCasesAsObjectsArray(IEnumerable<TokenizationTestCase> testCases) => testCases.Select(testCase => new object[] { testCase });
    }
}
