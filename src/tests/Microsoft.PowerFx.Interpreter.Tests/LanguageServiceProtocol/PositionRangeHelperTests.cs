// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;
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

        [Theory]
        [InlineData("{1}", 1)]
        [InlineData("12{3}45", 3)]
        [InlineData("1234{5}", 5)]
        [InlineData("123\n1{2}3", 2)]
        [InlineData("123\n{5}67", 1)]
        [InlineData("123\n5{6}7", 2)]
        [InlineData("123\n56{7}", 3)]
        [InlineData("123\n567{\n}890", 3)]
        public void TestGetCharPosition(string expression, int expected)
        {
            var pattern = @"\{[0-9|\n]\}";
            var re = new Regex(pattern);
            var matches = re.Matches(expression);
            Assert.Single(matches);

            var position = matches[0].Index;
            expression = expression.Substring(0, position) + expression[position + 1] + expression.Substring(position + 3);

            Assert.Equal(expected, PositionRangeHelper.GetCharPosition(expression, position));
        }

        [Fact]
        public void TestGetPosition()
        {
            Assert.Equal(0, PositionRangeHelper.GetPosition("123", 0, 0));
            Assert.Equal(1, PositionRangeHelper.GetPosition("123", 0, 1));
            Assert.Equal(2, PositionRangeHelper.GetPosition("123", 0, 2));
            Assert.Equal(4, PositionRangeHelper.GetPosition("123\n123456\n12345", 1, 0));
            Assert.Equal(5, PositionRangeHelper.GetPosition("123\n123456\n12345", 1, 1));
            Assert.Equal(11, PositionRangeHelper.GetPosition("123\n123456\n12345", 2, 0));
            Assert.Equal(13, PositionRangeHelper.GetPosition("123\n123456\n12345", 2, 2));
            Assert.Equal(3, PositionRangeHelper.GetPosition("123", 0, 999));
        }
    }
}
