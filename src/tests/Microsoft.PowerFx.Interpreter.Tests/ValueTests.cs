// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

// Need to test against some poorly formed classes that violate StyleCop rules. 
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ValueTests
    {
        private static readonly TypeMarshallerCache _cache = new TypeMarshallerCache();

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void Bool(bool val, string expectedStr)
        {
            var formulaValue = FormulaValue.New(val);

            Assert.Equal(val, formulaValue.Value);
            Assert.Equal(val, formulaValue.ToObject());

            Assert.Equal(FormulaType.Boolean, formulaValue.Type);

            var resultStr = formulaValue.Dump();
            Assert.Equal(expectedStr, resultStr);
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(-5, "-5")]
        public void Number(double val, string expectedStr)
        {
            var formulaValue = FormulaValue.New(val);

            Assert.Equal(val, formulaValue.Value);
            Assert.Equal(val, formulaValue.ToObject());

            Assert.Equal(FormulaType.Number, formulaValue.Type);

            var resultStr = formulaValue.Dump();
            Assert.Equal(expectedStr, resultStr);

            // Nullable overloads
            double? val2 = val;
            var formulaValue2 = FormulaValue.New((double?)val); // nullable overload
            Assert.Equal(expectedStr, formulaValue2.Dump());

            var formulaValue3 = FormulaValue.New((double?)null);
            Assert.IsType<NumberType>(formulaValue3.Type);
            Assert.IsType<BlankValue>(formulaValue3);
        }

        [Theory]
        [InlineData("abc", "\"abc\"")]
        public void String(string val, string expectedStr)
        {
            var formulaValue = FormulaValue.New(val);

            Assert.Equal(val, formulaValue.Value);
            Assert.Equal(val, formulaValue.ToObject());

            Assert.Equal(FormulaType.String, formulaValue.Type);

            var resultStr = formulaValue.Dump();
            Assert.Equal(expectedStr, resultStr);
        }

        [Fact]
        public void Record()
        {
            RecordValue r = FormulaValue.NewRecordFromFields(
                new NamedValue("Num", FormulaValue.New(15)),
                new NamedValue("Str", FormulaValue.New("hello")));

            RecordType rt = (RecordType)r.Type;

            // Access as a dynamic
            dynamic d = r.ToObject();
            Assert.Equal(15, d.Num);
            Assert.Equal("hello", d.Str);

            // Explicit field lookup 
            var numField = r.GetField("Num");
            Assert.Equal(15.0, ((NumberValue)numField).Value);

            // Get json runtime representation
            var resultStr = r.Dump();
            Assert.Equal("{Num:15,Str:\"hello\"}", resultStr);
        }

        [Fact]
        public void RecordMarshaller()
        {
            var obj = new TestRowPrimitives
            {
                numberInt = 15,
                numberDouble = 15.1,
                boolean = true,
                str = "hello",
                datetime = new DateTime(1999, 3, 1),
                timespan = TimeSpan.FromDays(3)
            };

            RecordValue r = _cache.NewRecord(obj);
            dynamic d = r.ToObject();

            Assert.Equal((double)obj.numberInt, d.numberInt);
            Assert.Equal(obj.numberDouble, d.numberDouble);
            Assert.Equal(obj.boolean, d.boolean);
            Assert.Equal(obj.str, d.str);
            Assert.Equal(obj.datetime, d.datetime);
            Assert.Equal(obj.timespan, d.timespan);
        }

        // Test the different types of primitives that can get marshalled. 
        private class TestRowPrimitives
        {
            public int numberInt { get; set; }

            public double numberDouble { get; set; }

            public bool boolean { get; set; }

            public string str { get; set; }

            public DateTime datetime { get; set; }

            public DateTimeOffset dto { get; set; }

            public TimeSpan timespan { get; set; }
        }

        [Fact]
        public void RecordNotMarshalled()
        {
            RecordValue r = _cache.NewRecord(new RowDontMarshal());
            Assert.Empty(r.Fields);
        }

        // These member kinds are not marshalled.  
        private class RowDontMarshal
        {
            public static int StaticProp { get; set; }

            private int PrivateProp { get; set; }

            internal int InternalProp { get; set; }

#pragma warning disable CS0649 // Unassigned field is intended here to test marshalling
            public int publicField;
#pragma warning restore CS0649
        }

        private class TestRow
        {
            public double a { get; set; }

            public string str { get; set; }
        }

        [Fact]
        public void Table()
        {
            TableValue val = _cache.NewTable(
                new TestRow { a = 10, str = "alpha" },
                new TestRow { a = 15, str = "beta" });

            var field1 = ((StringValue)((RecordValue)val.Index(2).Value).GetField("str")).Value;
            Assert.Equal("beta", field1);

            dynamic d = val.ToObject();
            Assert.Equal(15.0, d[1].a);

            // Verify runtime json
            var resultStr = val.Dump();

            Assert.Equal("Table({a:10,str:\"alpha\"},{a:15,str:\"beta\"})", resultStr);
        }

        [Fact]
        public void TableFromRecords()
        {
            RecordValue r1 = _cache.NewRecord(new TestRow { a = 10, str = "alpha" });
            RecordValue r2 = _cache.NewRecord(new TestRow { a = 15, str = "beta" });
            TableValue val = FormulaValue.NewTable(r1.Type, r1, r2);

            var result1 = ((RecordValue)val.Index(2).Value).GetField("a").ToObject();
            Assert.Equal(15.0, result1);

            dynamic d = val.ToObject();
            Assert.Equal(10.0, d[0].a);

            // Verify runtime json
            var resultStr = val.Dump();
            Assert.Equal("Table({a:10,str:\"alpha\"},{a:15,str:\"beta\"})", resultStr);

            TableValue val2 = NewTableT(r1, r2);
            Assert.Equal(resultStr, val2.Dump());
        }

        // Heterogenous table.
        [Fact]
        public void TableFromMixedRecords()
        {
            var cache = new TypeMarshallerCache();
            RecordValue r1 = _cache.NewRecord(new { a = 10, b = 20, c = 30 });
            RecordValue r2 = _cache.NewRecord(new { a = 11, c = 31 });
            TableValue val = FormulaValue.NewTable(r1.Type, r1, r2);

            // Users first type 

            var result1 = ((RecordValue)val.Index(2).Value).GetField("a").ToObject();
            Assert.Equal(11.0, result1);

            var result2 = ((RecordValue)val.Index(2).Value).GetField("b");
            Assert.IsType<BlankValue>(result2);
            Assert.IsType<NumberType>(result2.Type);
        }

        [Fact]
        public void EmptyTableFromRecords()
        {
            // Empty means we can't infer the type from the records passed in. 
            TableValue val = _cache.NewTable(new RecordValue[0]);

            Assert.Empty(val.Rows);

            var resultStr = val.Dump();
            Assert.Equal("Table()", resultStr);
        }

        // Helper to bypass function overloading and invoke the generic overload. 
        private static TableValue NewTableT<T>(params T[] rows)
        {
            return _cache.NewTable<T>(rows);
        }

        // Single Column Table
        [Fact]
        public void TableFromPrimitive()
        {
            NumberValue r1 = FormulaValue.New(10);
            NumberValue r2 = FormulaValue.New(20);
            TableValue val = FormulaValue.NewSingleColumnTable(r1, r2);

            dynamic d = val.ToObject();
            Assert.Equal(20.0, d[1]); // SCT returned as arrays

            // Verify runtime resultStr
            var resultStr = val.Dump();

            Assert.Equal("Table({Value:10},{Value:20})", resultStr);

            // Must use NewSingleColumnTable to create a single column table.
            Assert.Throws<InvalidOperationException>(() => NewTableT(r1, r2));
        }

        [Fact]
        public void SingleColumnTable()
        {
            TableValue value = (TableValue)FormulaValueJSON.FromJson("[1,2,3]");

            TableType type = (TableType)value.Type;

            TableType typeExpected = TableType.Empty()
                .Add(new NamedFormulaType("Value", FormulaType.Number));
            Assert.Equal(typeExpected, type);

            // Another way to compare
            var field1 = type.GetFieldTypes().First();
            Assert.Equal("Value", field1.Name);
            Assert.Equal(FormulaType.Number, field1.Type);

            RecordValue row0 = value.Rows.First().Value;
            Assert.Equal(1.0, row0.GetField("Value").ToObject());

            var len = value.Rows.Count();
            Assert.Equal(3, len);

            // Converts to single column 
            var obj = value.ToObject();

            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, (ICollection)obj);

            var resultStr = value.Dump();
            Assert.Equal("Table({Value:1},{Value:2},{Value:3})", resultStr);
        }

        [Fact]
        public void Blanks()
        {
            var value = _cache.Marshal(null, typeof(int));
            Assert.True(value is BlankValue);

            // null marshals as blank. 
            RecordValue r = _cache.NewRecord(new
            {
                number = 15.1,
                missing = (string)null,
            });

            Assert.True(r.GetField("missing") is BlankValue);
            Assert.Equal(15.1, r.GetField("number").ToObject());
        }

        [Fact]
        public void DeriveFromValidFormulaValue()
        {
            // Only Blank and Error can derive from FormulaValue directly.
            // All else should derive from ValidFormulaValue. 
            // See ValidFormulaValue for explanation. 
            var set = new HashSet<Type>
            {
                typeof(BlankValue),
                typeof(ErrorValue),
                typeof(ValidFormulaValue),
                typeof(LambdaFormulaValue), // Special, can eval to any FormulaValue.
            };

            var asmInterpreter = typeof(RecalcEngine).Assembly;
            var asmCore = typeof(Engine).Assembly;
            var allTypes = asmInterpreter.GetTypes().Concat(asmCore.GetTypes());

            foreach (var type in allTypes)
            {
                if (type.BaseType == typeof(FormulaValue))
                {
                    Assert.True(set.Contains(type), $"Type {type.FullName} should derive from {typeof(ValidFormulaValue).FullName}, not FormulaValue.");
                }
            }
        }
    }

    public static class FormulaValueExtensions
    {
        public static string Dump(this FormulaValue value)
        {
            var sb = new StringBuilder();

            var settings = new FormulaValueSerializerSettings()
            {
                UseCompactRepresentation = true,
            };

            // Serializer will produce a human-friedly representation of the value
            value.ToExpression(sb, settings);

            return sb.ToString(); 
        }
    }
}
