// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CharacterUtilsTest : PowerFxTest
    {
        [Theory]
        [InlineData("test", "test")]
        [InlineData(
            "\\\\ \\\" \\\' \\0 \\b \\t \\n \\v \\f \\r \\u0085 \\u2028 \\u2029",
            "\\ \" \' \0 \b \t \n \v \f \r \u0085 \u2028 \u2029")]
        public void EscapeStringTests(string expectedEscapeString, string inputEscapeStrig)
        {
            var actualEscapeString = CharacterUtils.Escape(inputEscapeStrig);
            Assert.Equal(expectedEscapeString, actualEscapeString);
        }

        [Theory]
        [InlineData("_1_2C6D__a", "1Ɑ_a")]
        [InlineData("_2C6D__", "Ɑ_")]
        [InlineData("test1", "test1")]
        public void ToJsIdentifierTests(string expected, string input)
        {
            var actual = CharacterUtils.ToJsIdentifier(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("a\"\"{{interpolation}}", "a\"{interpolation}", true)]
        [InlineData("test", "test", false)]
        public void ExcelEscapeStringTests(string expected, string input, bool isValueAnInterpolatedString)
        {
            var actual = CharacterUtils.ExcelEscapeString(input, isValueAnInterpolatedString);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("\"abc\"", "abc")]
        [InlineData(" ", " ")]
        public void ToPlainTextTests(string expected, string input)
        {
            var actual = CharacterUtils.ToPlainText(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("{{foo}}", "{foo}")]
        public void MakeSafeForFormatStringTests(string expected, string input)
        {
            var actual = CharacterUtils.MakeSafeForFormatString(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HasSpacesTest()
        {
            var actual = CharacterUtils.HasSpaces(" ");
            Assert.True(actual);
        }
    }
}
