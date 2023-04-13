// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class FunctionCompilationTests : PowerFxTest
    {
        [Theory]
        [InlineData("Switch(A, 2, \"two\", \"other\")")]
        [InlineData("IfError(Text(A), Switch(FirstError.Kind, ErrorKind.Div0, \"Division by zero\", ErrorKind.Numeric, \"Numeric error\", \"Other error\"))")]
        public void TestSwitchFunctionCompilation(string expression)
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", 15m);
            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.String, check.ReturnType);
        }
    }
}
