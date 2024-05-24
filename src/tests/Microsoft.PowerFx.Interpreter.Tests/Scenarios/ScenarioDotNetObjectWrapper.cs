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

            public long BigInt { get; set; }

            public decimal Decimal { get; set; }

            public decimal Decimal2 { get; set; }

            public double Double { get; set; }

            public uint UInt { get; set; }

            public ulong ULong { get; set; }

            // Verify we don't eagerly touch all properties
            public string Fail => throw new NotImplementedException("Don't call this");
        }

        private static void Add(RecalcEngine engine, string name, object obj)
        {
            var objFx = PrimitiveWrapperAsUnknownObject.New(obj);
            engine.UpdateVariable(name, objFx);
        }

        // inline data does not support decimal, which is why the max/min decimal tests aren't here
        [Theory]
        [InlineData("Float(obj.Next.Value)", 20.0)]
        [InlineData("Float(obj.Next.Double)", 2.28515625)]
        [InlineData("Float(obj.UInt)", (double)0xffffffffU)]
        [InlineData("Float(obj.Next.UInt)", (double)0xfffffff0U)]
        [InlineData("Decimal(obj.ULong)", 0xffffffffffffffffUL)]
        [InlineData("Decimal(obj.Next.ULong)", 0xfffffffffffffff0UL)]
        [InlineData("Decimal(obj.BigInt)", -9223372036854775808L)]
        [InlineData("Decimal(obj.Next.BigInt)", 9223372036854775807L)]
        [InlineData("Decimal(obj.Decimal2)", 9882075136L)]
        [InlineData("Decimal(obj.Next.Decimal2)", 158113202176L)]
        [InlineData("Text(obj.Value)", "10")]
        [InlineData("Text(obj.BigInt)", "-9223372036854775808")]
        [InlineData("Text(obj.Decimal)", "-79228162514264337593543950335")]
        [InlineData("Text(obj.Double)", "0.5712890625")]
        [InlineData("Text(obj.Next.Value)", "20")]
        [InlineData("Text(obj.Next.BigInt)", "9223372036854775807")]
        [InlineData("Text(obj.Next.Decimal)", "79228162514264337593543950335")]
        [InlineData("Text(obj.Next.Double)", "2.28515625")]
        [InlineData("Text(obj.UInt)", "4294967295")]
        [InlineData("Text(obj.Next.UInt)", "4294967280")]
        [InlineData("Text(obj.ULong)", "18446744073709551615")]
        [InlineData("Text(obj.Next.ULong)", "18446744073709551600")]
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
                Double = 0.5712890625,
                BigInt = -9223372036854775808L,
                Decimal = -79228162514264337593543950335m,
                Decimal2 = 9882075136,
                UInt = 0xFFFFFFFF,
                ULong = 0xFFFFFFFFFFFFFFFFUL,
                Next = new TestObj
                {
                    Value = 20,
                    Double = 2.28515625,
                    BigInt = 9223372036854775807L,
                    Decimal = 79228162514264337593543950335m,
                    Decimal2 = 158113202176,
                    UInt = 0xFFFFFFF0,
                    ULong = 0xFFFFFFFFFFFFFFF0UL
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
            else if (expected is long || expected is ulong)
            {
                Assert.Equal(Convert.ToDecimal(expected), result.ToObject());
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
            var parameters = FormulaValue.NewRecordFromFields(
                new NamedValue("obj2", objFx2));

            var result = engine.Eval("obj2.Next", parameters);

            Assert.IsType<UntypedObjectValue>(result);
            var uov = (UntypedObjectValue)result;
            var obj1result = ((PrimitiveWrapperAsUnknownObject)uov.Impl)._source;
            
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
    }
}
