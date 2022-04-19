// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

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
            var config = new PowerFxConfig(powerFxFlags: PowerFxFlags.EnableExpressionChaining);            
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
                "Assert2(obj.prop, 123); Set2(obj, \"prop\", 456); Assert2(obj.prop, 456)"
            };

            foreach (var expr in exprs)
            {
                var x = engine.Eval(expr); // Assert failures will throw.
            }
        }

        private class Assert2Function : ReflectionFunction
        {
            public Assert2Function()
                : base("Assert2", FormulaType.Blank, new UntypedObjectType(), FormulaType.Number)
            {
            }

            public void Execute(UntypedObjectValue obj, NumberValue val)
            {
                var impl = obj.Impl;
                var actual = impl.GetDouble();
                var expected = val.Value;

                if (actual != expected)
                {
                    throw new InvalidOperationException($"Mismatch");
                }
            }
        }

        private class Set2Function : ReflectionFunction
        {
            public Set2Function()
                : base(
                      "Set2",
                      FormulaType.Blank,  // returns
                      new UntypedObjectType(),
                      FormulaType.String,
                      FormulaType.Number) // $$$ Any?
            {
            }

            public void Execute(UntypedObjectValue obj, StringValue propName, FormulaValue val)
            {
                var impl = (MutableObject)obj.Impl;
                impl.Set(propName.Value, val);
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
