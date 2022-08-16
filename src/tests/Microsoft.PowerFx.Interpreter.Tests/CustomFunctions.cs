// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class CustomFunctions : PowerFxTest
    {
        [Fact]
        public void CustomFunction()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCustomFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetAllFunctionNames().First(name => name == "TestCustom");
            Assert.NotNull(func);

            // Can be invoked. 
            var result = engine.Eval("TestCustom(3,true)");
            Assert.Equal("3,True", result.ToObject());
        }

        // Must have "Function" suffix. 
        private class TestCustomFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            public static StringValue Execute(NumberValue x, BooleanValue b)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        public class TestObj
        {
            public double NumProp { get; set; }

            public bool BoolProp { get; set; }
        }

        private static readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public void CustomSetPropertyFunction()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCustomSetPropFunction());
            var engine = new RecalcEngine(config);

            var obj = new TestObj();
            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(obj);
            engine.UpdateVariable("x", x);

            // Test multiple overloads
            engine.Eval("SetProperty(x.NumProp, 123)", options: _opts);
            Assert.Equal(123.0, obj.NumProp);

            engine.Eval("SetProperty(x.BoolProp, true)", options: _opts);
            Assert.True(obj.BoolProp);

            // Test failure cases
            var check = engine.Check("SetProperty(x.BoolProp, true)"); // Binding Fail, behavior prop 
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.BoolProp, 123)"); // arg mismatch
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.numMissing, 123)", options: _opts); // Binding Fail
            Assert.False(check.IsSuccess);
        }

        // Must have "Function" suffix. 
        private class TestCustomSetPropFunction : ReflectionFunction
        {
            public TestCustomSetPropFunction()
                : base(SetPropertyName, FormulaType.Boolean)
            {
            }

            // Must have "Execute" method. 
            public static BooleanValue Execute(RecordValue source, StringValue propName, FormulaValue newValue)
            {
                var obj = (TestObj)source.ToObject();

                // Use reflection to set
                var prop = obj.GetType().GetProperty(propName.Value);
                prop.SetValue(obj, newValue.ToObject());

                return FormulaValue.New(true);
            }
        }

        // Verify we can add overloads of a custom function. 
        [Theory]
        [InlineData("SetField(123)", "SetFieldNumberFunction,123")]
        [InlineData("SetField(\"-123\")", "SetFieldStrFunction,-123")]
        [InlineData("SetField(\"abc\")", "SetFieldStrFunction,abc")]
        [InlineData("SetField(true)", "SetFieldNumberFunction,1")] // true coerces to number 1
        [InlineData("SetField(Date(2040, 01, 01))", "SetFieldDateFunction,1/1/2040")]
        [InlineData("SetField(Date(2040, 01, 01))", "SetFieldDateTimeFunction,1/1/2040 12:00:00 AM")]
        public void Overloads(string expr, string expected)
        {
            var config = new PowerFxConfig();
            config.AddFunction(new SetFieldNumberFunction());
            config.AddFunction(new SetFieldStrFunction());
            config.AddFunction(new SetFieldDateFunction());
            config.AddFunction(new SetFieldDateTimeFunction());
            var engine = new RecalcEngine(config);

            var count = engine.GetAllFunctionNames().Count(name => name == "SetField");
            Assert.Equal(1, count); // no duplicates

            // Duplicates?
            var result = engine.Eval(expr);
            var actual = ((StringValue)result).Value;

            Assert.Equal(expected, actual);
        }

        private abstract class SetFieldBaseFunction : ReflectionFunction
        {
            public SetFieldBaseFunction(FormulaType fieldType) 
                : base("SetField", FormulaType.String, fieldType)
            {                
            }

            public StringValue Execute(FormulaValue newValue)
            {
                var overload = GetType().Name;
                var result = overload + "," + newValue.ToObject().ToString();
                return FormulaValue.New(result);
            }
        }

        private class SetFieldNumberFunction : SetFieldBaseFunction
        {
            public SetFieldNumberFunction()
                : base(FormulaType.Number)
            {
            }
        }

        private class SetFieldStrFunction : SetFieldBaseFunction
        {
            public SetFieldStrFunction()
                : base(FormulaType.String)
            {
            }
        }

        private class SetFieldDateTimeFunction : SetFieldBaseFunction
        {
            public SetFieldDateTimeFunction()
                : base(FormulaType.DateTime)
            {
            }
        }

        private class SetFieldDateFunction : SetFieldBaseFunction
        {
            public SetFieldDateFunction()
                : base(FormulaType.Date)
            {
            }
        }
    }
}
