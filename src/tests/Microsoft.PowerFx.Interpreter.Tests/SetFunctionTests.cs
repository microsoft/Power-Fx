// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SetFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        // $$$ Trigger a recalc?
        [Fact]
        public void TestSetVar()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            var r1 = engine.Eval("x", null, _opts); // 12
            Assert.Equal(12.0, r1.ToObject());

            var r2 = engine.Eval("Set(x, 15)", null, _opts);

            r1 = engine.Eval("x"); // 15
            Assert.Equal(15.0, r1.ToObject());

            r1 = engine.GetValue("x");
            Assert.Equal(15.0, r1.ToObject());
        }

        // Test various failure cases 
        [Fact]
        public void TestSetVarFailures()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            // Fails, can't set var that wasn't already declared
            var result = engine.Check("Set(missing, 12)");
            Assert.False(result.IsSuccess);

            // Fails, behavior function must be in behavior context. 
            result = engine.Check("Set(x, true)");
            Assert.False(result.IsSuccess);

            // Fails, type mismatch
            result = engine.Check("Set(x, true)", null, _opts);
            Assert.False(result.IsSuccess);

            // Fails, arg0 is not a settable object. 
            result = engine.Check("Set({y:x}.y, 20)", null, _opts);
            Assert.False(result.IsSuccess);
        }

        // Set() can only be called if it's enabled.
        [Fact]
        public void TestSetVarFailureEnabled()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            var result = engine.Check("Set(x, 15)", null, _opts);
            Assert.False(result.IsSuccess);
        }
    }
}
