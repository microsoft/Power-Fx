// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class IRPrintTest : PowerFxTest
    {
        [Theory]
        [InlineData("$\"\"", "\"\":s")] // empty interpolation dont call Concatenate function
        [InlineData("$\"She is {100} years old\"", "Concatenate:s(\"She is \":s, DecimalToText:s(100:w), \" years old\":s)")]
        public void IRStringInterpolationTest(string expression, string expected)
        {
            var check = new CheckResult(new Engine());
            var ir = check.SetText(expression)
                .SetBindingInfo()
                .PrintIR();
            
            Assert.Equal(expected, ir);
        }
    }
}
