// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Tests;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate mutation example using IUntypedObject
    public class ScenarioMutation : PowerFxTest
    {       
        [Fact]
        public void MutabilityTest()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new Assert2Function());
            config.AddFunction(new Set2Function());
            var engine = new RecalcEngine(config);

            var d = new Dictionary<string, FormulaValue>
            {
                ["prop"] = FormulaValue.New(123)
            };
                        
            var obj = MutableObject.New(d);
            engine.UpdateVariable("obj", obj);

            var exprs = new string[]
            {
                "Assert2(obj.prop, 123)",
                "Set2(obj, \"prop\", 456)",
                "Assert2(obj.prop, 456)"
            };

            foreach (var expr in exprs)
            {
                var x = engine.Eval(expr); // Assert failures will throw.
            }
        }

        [Fact]
        public void MutabilityTest_Chain()
        {
            var config = new PowerFxConfig();            
            config.AddFunction(new Assert2Function());
            config.AddFunction(new Set2Function());
            config.AddFunction(new Set3Function());
            var engine = new RecalcEngine(config);            
            var exprs = new string[]
            {
                "Assert2(obj.prop, 123); Set2(obj, \"prop\", 456); Assert2(obj.prop, 456)",
                "Assert2(obj.prop, 123); Set3(obj, \"prop\", \"prop2\"); Assert2(obj.prop, 456)"
            };

            foreach (var expr in exprs)
            {
                var d = new Dictionary<string, FormulaValue>
                {
                    ["prop"] = FormulaValue.New(123),
                    ["prop2"] = FormulaValue.New(456)
                };

                var obj = MutableObject.New(d);
                engine.UpdateVariable("obj", obj);

                var x = engine.Eval(expr, options: new ParserOptions() { AllowsSideEffects = true }); // Assert failures will throw.

                if (x is ErrorValue ev)
                {
                    throw new XunitException($"FormulaValue is ErrorValue: {string.Join("\r\n", ev.Errors)}");
                }

                Assert.IsType<NumberValue>(x);
                Assert.Equal(456, ((NumberValue)x).Value);
                Assert.Equal(456, ((NumberValue)d["prop"]).Value);
            }            
        }

        [Fact]
        public async Task MutabilityTest_Chain2()
        {
            var config = new PowerFxConfig();
            var verify = new AsyncVerify();            
            var asyncHelper = new AsyncFunctionsHelper(verify);  
            config.AddFunction(asyncHelper.GetFunction());
            var waitForHelper = new WaitForFunctionsHelper(verify); 
            config.AddFunction(waitForHelper.GetFunction());
            var engine = new RecalcEngine(config);

            // Run in special mode that ensures we're not calling .Result
            var result = await verify.EvalAsync(engine, "WaitFor(0); WaitFor(1); WaitFor(2)", new ParserOptions() { AllowsSideEffects = true });
            

            int y = 0;

        }

        private class Assert2Function : ReflectionFunction
        {
            public Assert2Function()
                : base("Assert2", FormulaType.Number, new UntypedObjectType(), FormulaType.Number)
            {
            }

            public NumberValue Execute(UntypedObjectValue obj, NumberValue val)
            {
                var impl = obj.Impl;
                var actual = impl.GetDouble();
                var expected = val.Value;

                if (actual != expected)
                {
                    throw new InvalidOperationException($"Mismatch");
                }

                return new NumberValue(IRContext.NotInSource(FormulaType.Number), actual);
            }
        }

        private class Set2Function : ReflectionFunction
        {
            public Set2Function()
                : base("Set2", FormulaType.Blank, new UntypedObjectType(), FormulaType.String, FormulaType.Number)
            {
            }

            public void Execute(UntypedObjectValue obj, StringValue propName, FormulaValue val)
            {
                var impl = (MutableObject)obj.Impl;
                impl.Set(propName.Value, val);
            }
        }

        private class Set3Function : ReflectionFunction
        {
            public Set3Function()
                : base("Set3", FormulaType.Blank, new UntypedObjectType(), FormulaType.String, FormulaType.String)
            {
            }

            public void Execute(UntypedObjectValue obj, StringValue propName, StringValue propName2)
            {
                var impl = (MutableObject)obj.Impl;
                impl.TryGetProperty(propName2.Value, out var propValue);
                var val = propValue.GetDouble();                
                impl.Set(propName.Value, new NumberValue(IRContext.NotInSource(FormulaType.Number), val));
            }
        }

        private class SimpleObject : IUntypedObject
        {
            private readonly FormulaValue _value;

            public SimpleObject(FormulaValue value)
            {
                _value = value;
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => _value.Type;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                return ((BooleanValue)_value).Value;
            }

            public double GetDouble()
            {
                return ((NumberValue)_value).Value;
            }

            public string GetString()
            {
                return ((StringValue)_value).Value;
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                throw new NotImplementedException();
            }
        }

        private class MutableObject : IUntypedObject
        {
            private Dictionary<string, FormulaValue> _values = new Dictionary<string, FormulaValue>();

            public void Set(string property, FormulaValue newValue)
            {
                _values[property] = newValue;
            }

            public static UntypedObjectValue New(Dictionary<string, FormulaValue> d)
            {
                var x = new MutableObject()
                {
                    _values = d
                };

                return FormulaValue.New(x);
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => ExternalType.ObjectType;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                throw new NotImplementedException();
            }

            public string GetString()
            {
                throw new NotImplementedException();
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                if (_values.TryGetValue(value, out var x))
                {
                    result = new SimpleObject(x);
                    return true;
                }

                result = null;
                return false;
            }
        }
    }
}
