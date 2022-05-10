// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SetFunctionTests : PowerFxTest
    {
        [Fact]
        public void TestSetVar()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            var r1 = engine.Eval("x"); // 12
            Assert.Equal(12.0, r1.ToObject());

            var r2 = engine.Eval("Set(x, 15)");

            r1 = engine.Eval("x"); // 15
            Assert.Equal(15.0, r1.ToObject());

            r1 = engine.GetValue("x");
            Assert.Equal(15.0, r1.ToObject());

            var result = engine.Check("Set(x, true)"); // Fails, type mismatch
            Assert.False(result.IsSuccess);

            result = engine.Check("Set({y:x}.y, 20)"); // Fails, type mismatch
            Assert.False(result.IsSuccess);
        }
    }
}
