// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LexicalHelpersTests
    {
        [Theory]

        // Strings 
        [InlineData("hello", "$\"hello\"")]
        [InlineData("hello, {name}", "$\"hello, {name}\"")]
        [InlineData("1+2={1+2}", "$\"1+2={1+2}\"")]

        [InlineData("(123)", "$\"(123)\"")]
        [InlineData("1+2", "$\"1+2\"")]

        [InlineData("a quote \" end", "$\"a quote \"\" end\"")] // quotes are escaped
        [InlineData(" 1", "$\" 1\"")] // leading space means string

        // $$$ todo - need proper escaping inside of { } exprs. 

        // Equal mean expression 
        [InlineData("={x:5}", "{x:5}")] // record 

        // Single tick means string literal 
        [InlineData("'123", "\"123\"")]
        [InlineData("'=1+2", "\"=1+2\"")]
        [InlineData("'{name}", "\"{name}\"")]

        // Numbers
        [InlineData("123", "123")]
        [InlineData("-123", "-123")]
        [InlineData("00", "0")]

        // booleans
        [InlineData("true", "true")]
        [InlineData("tRUe", "true")] // case insensitive
        [InlineData("false", "false")]
        [InlineData("False", "false")]
        public void Test(string input, string expected)
        {
            var actual = LexicalHelpers.LiteralToExpression(input);
            Assert.Equal(expected, actual);
        }
    }
}
