// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol
{
    public class PositionRangeHelperTests
    {
        private readonly string _expression = "If(Len(PhoneNumber.Input) < 10,\r\nNotify(PhoneWarningText),\r\nCollect(PhoneNumner, {PhoneNumber: PhoneNumber.Input});";

        private readonly string _eol = "\r\n";

        [Theory]

        // Same Line Tests
        [InlineData(1, 1, 1, 4, true, 0, 3)]
        [InlineData(1, 1, 1, 4, false, 0, 3)]
        [InlineData(1, 4, 1, 30, true, 3, 29)]
        [InlineData(1, 4, 1, 30, false, 3, 29)]
        [InlineData(2, 10, 2, 27, true, 42, 59)]
        [InlineData(2, 10, 2, 27, false, 42, 59)]
        [InlineData(3, 32, 3, 55, true, 91, 114)]
        [InlineData(3, 32, 3, 55, false, 91, 114)]
        [InlineData(1, 1, 1, 1, true, 0, 0)]
        [InlineData(1, 1, 1, 1, false, 0, 0)]

        // Multi Line Tests
        [InlineData(1, 10, 3, 3, true, 9, 62)]
        [InlineData(1, 10, 3, 3, false, 9, 62)]
        [InlineData(1, 1, 3, 55, true, 0, 114)]
        [InlineData(1, 1, 3, 55, false, 0, 114)]
        [InlineData(2, 10, 3, 5, true, 42, 64)]
        [InlineData(2, 10, 3, 5, false, 42, 64)]

        // Start of the range invalid tests
        [InlineData(4, 20, 3, 55, true, -1, -1)]
        [InlineData(4, 20, 3, 55, false, -1, -1)]
        [InlineData(4, 2, 2, 10, true, -1, -1)]
        [InlineData(4, 2, 2, 10, false, -1, -1)]

        // Both ends of range are invalid tests
        [InlineData(10, 20, 20, 30, true, -1, -1)]
        [InlineData(10, 20, 20, 30, false, -1, -1)]

        // End of the range invalid tests
        [InlineData(2, 10, 20, 30, true, -1, -1)]
        [InlineData(2, 10, 20, 30, false, -1, -1)]
        [InlineData(3, 55, 20, 20, true, -1, -1)]
        [InlineData(3, 55, 20, 20, false, -1, -1)]

        // Invalid range tests
        [InlineData(2, 10, 1, 1, true, -1, -1)]
        [InlineData(2, 10, 1, 1, false, -1, -1)]
        public void TestRangesAreConvertedCorrectlyToPositions(int startLine, int startLineCol, int endLine, int endLineCol, bool oneBasedRange, int startIndex, int endIndex)
        {
            // Arrange
            if (!oneBasedRange)
            {
                startLine--;
                startLineCol--;
                endLine--;
                endLineCol--;
            }

            var range = SemanticTokensRelatedTestsHelper.CreateRange(startLine, endLine, startLineCol, endLineCol);
       
            // Act
            var result = PositionRangeHelper.ConvertRangeToPositions(range, _expression, _eol, oneBasedRange);

            // Assert
            Assert.Equal(startIndex, result.startIndex);
            Assert.Equal(endIndex, result.endIndex);
        }
    }
}
