// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
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

            public bool Flag { get; set; }

            public string Msg { get; set; }

            // Verify we don't eagerly touch all properties
            public string Fail => throw new NotImplementedException("Don't call this");
        }

        private static void Add(RecalcEngine engine, string name, object obj)
        {
            var objFx = PrimitiveWrapperAsUnknownObject.New(obj);
            engine.UpdateVariable(name, objFx);
        }

        [Theory]
        [InlineData("Value(obj.Next.Value)", 20.0)]
        [InlineData("Text(obj.Value)", "10")]
        [InlineData("obj.missing", null)] // missing fields are blank
        [InlineData("IsBlank(obj.Next.Next)", true)]
        [InlineData("IsBlank(obj.Next)", false)]
        [InlineData("obj.Next.Next", null)]
        [InlineData("Boolean(obj.Flag)", true)]
        [InlineData("Text(obj.Msg)", "xyz")]
        [InlineData("IsBlank(Index(array, 3))", true)]
        [InlineData("Text(Index(array, 2))", "two")]
        [InlineData("Index(array, 0)", "#error")] // Out of bounds, low
        [InlineData("Index(array, -1)", "#error")] // Out of bounds, low
        [InlineData("Index(array, 100)", "#error")] // Out of bounds, high
        public void Test(string expr, object expected)
        {
            var engine = new RecalcEngine();

            var obj = new TestObj
            {
                Value = 10,
                Next = new TestObj
                {
                    Value = 20
                },
                Flag = true,
                Msg = "xyz"
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
            var objFx2 = PrimitiveWrapperAsUnknownObject.New(obj2);

            // We can pass UntypedObject as a parameter.             
            var parameters = FormulaValue.NewRecordFromFields(new NamedValue("obj2", objFx2));

            var result = engine.Eval("obj2.Next", parameters);

            Assert.IsType<UntypedObjectValue>(result);
            var uov = (UntypedObjectValue)result;
            var obj1result = ((PrimitiveWrapperAsUnknownObject)uov.Implementation)._source;
            
            // And also ensure we get it back out with reference identity. 
            Assert.True(ReferenceEquals(obj1result, obj1));
        }
    }
}
