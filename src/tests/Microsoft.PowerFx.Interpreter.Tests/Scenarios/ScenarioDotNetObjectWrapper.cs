// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate lazy dynamic marshalling 
    public class ScenarioDotNetObjectWrapper : PowerFxTest
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
        [InlineData("obj.missing", null)] // missing fields are blank
        [InlineData("IsBlank(obj.Next.Next)", true)]
        [InlineData("IsBlank(obj.Next)", false)]
        [InlineData("obj.Next.Next", null)]
        [InlineData("IsBlank(Index(array, 3))", true)]
        [InlineData("Text(Index(array, 2))", "two")]
        [InlineData("Index(array, 0)", "#error")] // Out of bounds, low
        [InlineData("Index(array, -1)", "#error")] // Out of bounds, low
        [InlineData("Index(array, 100)", "#error")] // Out of bounds, high
        [InlineData("Text(obj.Value)", "#error")] // cast error. 
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

            if (expected is string str && str == "#error")
            {
                Assert.IsType<ErrorValue>(result);
            }
            else
            {
                Assert.Equal(expected, result.ToObject());
            }
        }

        [Fact]
        public void PassThrough()
        {            
            var obj1 = new TestObj { Value = 15 };
            var obj2 = new TestObj { Value = 25, Next = obj1 };

            var engine = new RecalcEngine();
            var objFx2 = FormulaValue.New(new Wrapper(obj2));

            // We can pass UntypedObject as a parameter.             
            var parameters = FormulaValue.NewRecordFromFields(
                new NamedValue("obj2", objFx2));

            var result = engine.Eval("obj2.Next", parameters);

            Assert.IsType<UntypedObjectValue>(result);
            var uov = (UntypedObjectValue)result;
            var obj1result = ((Wrapper)uov.Impl)._source;
            
            // And also ensure we get it back out with reference identity. 
            Assert.True(ReferenceEquals(obj1result, obj1));
        }

        [Fact]
        public void ObjectTypes()
        {
            // FormulaType.UntypedObject is any value wrapping a IUntypedObject
            // IUntypedObject can represent any Fx type as well as foriegn types. 
            // ExternalType.ObjectType is the set of IUntypedObject that represent a foriegn object 
            Assert.NotEqual(FormulaType.UntypedObject, ExternalType.ObjectType);
        }

        // Wrap a .net object. 
        // This will lazily marshal through the object as it's accessed.
        [DebuggerDisplay("{_source}")]
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
                    if (_source is int || _source is double)
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

                    // Fx infastructure already did this check,
                    // so we're only invoked in success case. 
                    Assert.True(index >= 0 && index <= a.Length);

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
                // Fx will only call this helper for numbers. 
                Assert.True(Type == FormulaType.Number);

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
                Assert.True(Type == FormulaType.String);

                if (_source is string valString)
                {
                    return valString;
                }

                throw new InvalidOperationException($"Not a string type");
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                Assert.True(Type == ExternalType.ObjectType);

                var t = _source.GetType();
                var prop = t.GetProperty(value, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    // Fx semantics are to return blank for missing properties. 
                    // No way to signal error here. 
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
