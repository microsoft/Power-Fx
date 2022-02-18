// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate lazy dynamic marshalling 
    public class ScenarioDotNetObjectWrapper
    {
        private class TestObj
        {
            public int Value { get; set; }

            // Recursion, but handled since we're dynamically marshalling. 
            public TestObj Next { get; set; }

            // Verify we don't eagerly touch all properties
            public string Fail => throw new NotImplementedException("Don't call this");
        }

        private static void Add(RecalcEngine engine, string name, object obj)
        {
            var objFx = FormulaValue.New(new Wrapper(obj));
            engine.UpdateVariable(name, objFx);
        }

        [Theory]        
        [InlineData("Value(obj.Next.Value)", 20.0)]
        [InlineData("obj.missing", null)]
        [InlineData("IsBlank(obj.Next.Next)", true)]
        [InlineData("obj.Next.Next", null)]
        [InlineData("IsBlank(Index(array, 3))", true)]
        [InlineData("Text(Index(array, 2))", "two")]
        public void Test(string expr, object expected)
        {
            var engine = new RecalcEngine();

            var obj = new TestObj
            {
                Value = 10,
                Next = new TestObj
                {
                    Value = 20
                }
            };
            Add(engine, "obj", obj);

            var array = new string[] { "one", "two", null, "four" };            
            Add(engine, "array", array);

            var result = engine.Eval(expr);

            Assert.Equal(expected, result.ToObject());
        }   

        // Wrap a .net object. 
        // This will lazily marshal through the object as it's accessed.
        private class Wrapper : IUntypedObject
        {
            public readonly object _source;

            public Wrapper(object source)
            {
                _source = source;
            }

            public FormulaType Type
            {
                get
                {
                    if (_source is int)
                    {
                        return FormulaType.Number;
                    }

                    if (_source is string)
                    {
                        return FormulaType.String;
                    }

                    if (_source.GetType().IsArray)
                    {
                        return ExternalType.ArrayType;
                    }

                    return ExternalType.ObjectType;
                }
            }

            public IUntypedObject this[int index]
            {
                get
                {
                    var a = (Array)_source;
                    var value = a.GetValue(index);
                    if (value == null)
                    {
                        return null;
                    }

                    return new Wrapper(value);
                }
            }

            public int GetArrayLength()
            {
                var a = (Array)_source;
                return a.Length;
            }

            public bool GetBoolean()
            {
                throw new NotImplementedException();
            }

            public double GetDouble()
            {
                if (_source is int valInt)
                {
                    return valInt;
                }

                if (_source is double valDouble)
                {
                    return valDouble;
                }

                throw new InvalidOperationException($"Not a number type");
            }

            public string GetString()
            {
                if (_source is string valString)
                {
                    return valString;
                }

                throw new InvalidOperationException($"Not a string type");
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                var t = _source.GetType();
                var prop = t.GetProperty(value, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    result = null;
                    return false;
                }

                var obj = prop.GetValue(_source);
                
                if (obj == null)
                {
                    result = null;
                    return false;
                }
                else
                {
                    result = new Wrapper(obj);
                }

                return true;
            }
        }
    }
}
