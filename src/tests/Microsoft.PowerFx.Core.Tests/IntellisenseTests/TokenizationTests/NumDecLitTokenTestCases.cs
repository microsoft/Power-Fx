// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Tests.IntellisenseTests;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    internal class NumDecLitTokenTestCases
    {
        private static readonly IEnumerable<string> _numbers = new List<string> 
        { 
            "12",
            "12.e3",
            ".12",
            "12.",
            ".234",
            "2e3",
            "2e+2",
            "2.e-3",
            "2e-44",
            ".12E-34",
            "2.E-3",
            ".0",
            "000333",
        };

        public static IEnumerable<object[]> NumDecLiteralTestCasesAsObjects
        {
            get
            {
                var numLitTokens = _numbers.Select(number => TokenizationTestCase.Create(number, ExpectedToken.CreateNumLitToken(number.Length)));
                var decLitTokens = _numbers.Select(number => TokenizationTestCase.Create(number, new ParserOptions { NumberIsFloat = false }, ExpectedToken.CreateDecLitToken(number.Length)));
                return TokenizationTestCase.TestCasesAsObjectsArray(numLitTokens.Concat(decLitTokens));
            }
        }
    }
}
