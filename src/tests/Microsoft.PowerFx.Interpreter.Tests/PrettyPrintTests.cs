// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PrettyPrintTests
    {
        [Theory]
        [InlineData("((1 + 2) + 3)", "1 + 2 + 3")]
        [InlineData("(((1  +  2)) + 3)", "1 + 2 + 3")]
        [InlineData("((1  +  (2)) + (3))", "1 + 2 + 3")]
        [InlineData("((1  +  (2)) * 3) - (4 * 5)", "(1 + 2) * 3 + -(4 * 5)")]
        public void TestPrettyPrint(string expr, string expectedExpr)
        {
            var actual = PrettyPrint.GetPrettyPrint(expr);

            Assert.Equal(expectedExpr, actual);
        }
    }
}
