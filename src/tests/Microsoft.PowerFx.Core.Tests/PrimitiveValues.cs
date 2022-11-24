// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PrimitiveTests
    {
        // Test how .net types marshal to FormulaType
        [Theory]
        [InlineData(typeof(double), typeof(NumberType))]
        [InlineData(typeof(int), typeof(NumberType))]
        [InlineData(typeof(decimal), typeof(NumberType))]
        [InlineData(typeof(long), typeof(NumberType))]
        [InlineData(typeof(float), typeof(NumberType))]
        [InlineData(typeof(Guid), typeof(GuidType))]
        [InlineData(typeof(bool), typeof(BooleanType))]
        [InlineData(typeof(DateTime), typeof(DateTimeType))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeType))]
        [InlineData(typeof(TimeSpan), typeof(TimeType))]
        [InlineData(typeof(string), typeof(StringType))]
        [InlineData(typeof(object), null)] // no match
        public void TestAll(Type dotnetType, Type fxType)
        {
            var result = PrimitiveValueConversions.TryGetFormulaType(dotnetType, out var actualFxType);

            if (fxType == null)
            {
                Assert.False(result);
            }
            else
            {
                Assert.True(result);

                Assert.True(actualFxType.GetType() == fxType);

                var value = GetValue(dotnetType);
                var fxValue = PrimitiveValueConversions.Marshal(value, dotnetType);
                Assert.Equal(fxType, fxValue.Type.GetType());

                var expr = actualFxType.DefaultExpressionValue();
                var engine = new Engine(new PowerFxConfig());

                var check = engine.Check(expr);

                Assert.Equal(check.ReturnType, actualFxType);
            }
        }

        // Get an instance of the object we can test against the marhsaller. 
        private static object GetValue(Type t)
        {
            if (t == typeof(string))
            {
                return string.Empty;
            }

            if (t == typeof(Guid))
            {
                return Guid.Empty;
            }

            return Activator.CreateInstance(t);
        }

        // .Net dates map to FormulaType.DateTime, so need to explicitly test FormulaType.Date
        [Fact]
        public void TestDateOnly()
        {
            // Has non-zero time, 
            var d = new DateTime(2011, 3, 6);
            var t = new TimeSpan(5, 10, 23);
            var dt = d + t;
            var dto = new DateTimeOffset(dt);

            // Can't represent times as Dates. 
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(dt, FormulaType.Date));
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(dto, FormulaType.Date));

            Assert.Throws<ArgumentException>(() => FormulaValue.NewDateOnly(dt));

            var result1 = (DateTimeValue)PrimitiveValueConversions.Marshal(dt, FormulaType.DateTime);
            var result2 = FormulaValue.New(dt);
            AssertEqual(dt, result2);
            AssertEqual(result1, result2);

            var result3 = (DateTimeValue)PrimitiveValueConversions.Marshal(dto, FormulaType.DateTime);
            AssertEqual(result1, result3);

            // Success
            var result4 = FormulaValue.NewDateOnly(d);
            AssertEqual(d, result4);
            var result5 = (DateValue)PrimitiveValueConversions.Marshal(d, FormulaType.Date);
            AssertEqual(result4, result5);
        }

        [Fact]
        public void TestDateNoUTC()
        {
            var utc = new DateTime(2011, 3, 6, 0, 0, 0, DateTimeKind.Utc);

            Assert.Throws<ArgumentException>(() => FormulaValue.NewDateOnly(utc));
            Assert.Throws<ArgumentException>(() => FormulaValue.New(utc));
        }

        [Fact]
        public void TestBlank()
        {
            var x1 = FormulaValue.NewBlank();
            Assert.Equal(FormulaType.Blank, x1.Type);
            Assert.IsType<BlankValue>(x1);

            var x2 = FormulaValue.NewBlank(null);
            Assert.Equal(FormulaType.Blank, x2.Type);
            Assert.IsType<BlankValue>(x2);

            var t = FormulaType.String;
            var x3 = FormulaValue.NewBlank(t);
            Assert.Equal(t, x3.Type);
            Assert.IsType<BlankValue>(x3);

            var value = PrimitiveValueConversions.Marshal(null, FormulaType.Number);
            Assert.IsType<BlankValue>(value);
            Assert.IsType<NumberType>(value.Type);

            var value2 = PrimitiveValueConversions.Marshal(null, typeof(int));
            Assert.IsType<BlankValue>(value2);
            Assert.IsType<NumberType>(value2.Type);
        }

        [Fact]
        public void MarshalFailures()
        {
            // Mismatch value and type
            Assert.Throws<ArgumentException>(() => PrimitiveValueConversions.Marshal(3, typeof(string)));

            // Can't use primitive marshaller on complex types. 
            var complex = new { x = 15 };
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal(complex, complex.GetType()));

            var fxType = RecordType.Empty();
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal(complex, fxType));

            // Verify illegal combinations fail.
            Assert.Throws<InvalidCastException>(() => PrimitiveValueConversions.Marshal(3, FormulaType.String));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.Number));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.Date));
            Assert.Throws<InvalidOperationException>(() => PrimitiveValueConversions.Marshal("str", FormulaType.DateTime));
        }

        private static void AssertEqual<T>(T a, PrimitiveValue<T> b)
        {
            Assert.Equal(a, b.Value);
        }

        private static void AssertEqual<T>(PrimitiveValue<T> a, PrimitiveValue<T> b)
        {
            Assert.Equal(a.Value, b.Value);
        }
    }
}
