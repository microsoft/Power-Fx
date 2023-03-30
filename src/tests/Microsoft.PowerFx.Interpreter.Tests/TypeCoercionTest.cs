// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Test type coercion from FormualValue to target type 
    public class TypeCoercionTest : PowerFxTest
    {
        // From number to other types
        [Theory]
        [InlineData(1, "true", "1", "1", "1", "12/31/1899 12:00:00 AM")]
        [InlineData(0, "false", "0", "0", "0", "12/30/1899 12:00:00 AM")]
        [InlineData(44962, "true", "44962", "44962", "44962", "2/5/2023 12:00:00 AM")]
        public void TryCoerceFromNumberTest(double value, string exprBool, string exprNumber, string exprDecimal, string exprStr, string exprDateTime)
        {
            var inputValue = FormulaValue.New(value);
            TryCoerceToTargetTypes(inputValue, exprBool, exprNumber, exprDecimal, exprStr, exprDateTime);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.Boolean, exprBool != null, exprBool);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.String, exprStr != null, exprStr);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.DateTime, exprDateTime != null, exprDateTime);
        }

        // From string to other types
        [Theory]
        [InlineData("", null, null, null, "", null)]
        [InlineData("1", null, "1", "1", "1", null)]
        [InlineData("true", "true", null, null, "true", null)]
        [InlineData("false", "false", null, null, "false", null)]
        [InlineData("True", "true", null, null, "True", null)]
        [InlineData("False", "false", null, null, "False", null)]
        [InlineData("This is a string", null, null, null, "This is a string", null)]
        public void TryCoerceFromStringTest(string value, string exprBool, string exprNumber, string exprDecimal, string exprStr, string exprDateTime)
        {
            var inputValue = FormulaValue.New(value);
            TryCoerceToTargetTypes(inputValue, exprBool, exprNumber, exprDecimal, exprStr, exprDateTime);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.Boolean, exprBool != null, exprBool);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.String, exprStr != null, exprStr);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.DateTime, exprDateTime != null, exprDateTime);
        }

        // From boolean to other types
        [Theory]
        [InlineData(true, "true", "1", "1", "true", null)]
        [InlineData(false, "false", "0", "0", "false", null)]
        public void TryCoerceFromBooleanTest(bool value, string exprBool, string exprNumber, string exprDecimal, string exprStr, string exprDateTime)
        {
            var inputValue = FormulaValue.New(value);
            TryCoerceToTargetTypes(inputValue, exprBool, exprNumber, exprDecimal, exprStr, exprDateTime);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.Boolean, exprBool != null, exprBool);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.String, exprStr != null, exprStr);
            TryCoerceFromSourceTypeToTargetType(inputValue, FormulaType.DateTime, exprDateTime != null, exprDateTime);
        }

        // From dateTime to other types
        [Theory]
        [InlineData("2/5/2023", null, "44962", "44962", "2/5/2023 12:00 AM", "2/5/2023 12:00 AM")]
        public void TryCoerceFromDateTimeTest(string value, string exprBool, string exprNumber, string exprDecimal, string exprStr, string exprDateTime)
        {
            TryCoerceToTargetTypes(FormulaValue.New(DateTime.Parse(value)), exprBool, exprNumber, exprDecimal, exprStr, exprDateTime);
        }

        // From number to datetime, expects an exception
        [Theory]
        [InlineData(4496200)]
        [InlineData(4E8)]
        public void TryCoerceFromNumberExpectsExceptionTest(double value)
        {
            var inputValue = FormulaValue.New(value);

            Assert.Throws<CustomFunctionErrorException>(() => inputValue.TryCoerceTo(out DateTimeValue resultDateTime));
        }

        // From Guid to String
        [Theory]
        [InlineData("0f8fad5bd9cb469fa16570867728950e", "0f8fad5b-d9cb-469f-a165-70867728950e")]
        [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e", "0f8fad5b-d9cb-469f-a165-70867728950e")]
        public void TryCoerceFromGuidToStringTest(string value, string expectedValue)
        {
            GuidValue guidInput = new GuidValue(IRContext.NotInSource(FormulaType.Guid), new Guid(value));
            bool isSucceeded = guidInput.TryCoerceTo(out StringValue resultValue);
            Assert.True(isSucceeded);
            Assert.Equal(expectedValue, resultValue.Value);
        }

        // Test if it can coerce to String
        [Fact]
        public void CanCoerceToStringTest()
        {
            ColorValue colorInput = new ColorValue(IRContext.NotInSource(FormulaType.Color), System.Drawing.Color.Red);
            Assert.False(colorInput.CanCoerceToStringValue());

            GuidValue guidInput = new GuidValue(IRContext.NotInSource(FormulaType.Guid), Guid.NewGuid());
            Assert.True(guidInput.CanCoerceToStringValue());

            NumberValue numberInput = new NumberValue(IRContext.NotInSource(FormulaType.Number), 12);
            Assert.True(numberInput.CanCoerceToStringValue());
        }

        // From original type to target type
        [Fact]
        public void CanPotentiallyCoerceToTest()
        {
            Assert.True(FormulaType.Number.CanPotentiallyCoerceTo(FormulaType.String));
            Assert.True(FormulaType.DateTime.CanPotentiallyCoerceTo(FormulaType.Number));

            Assert.False(FormulaType.Color.CanPotentiallyCoerceTo(FormulaType.String));
            Assert.False(FormulaType.Number.CanPotentiallyCoerceTo(FormulaType.Hyperlink));

            RecordType inputType = RecordType.Empty()
                .Add(new NamedFormulaType("a", FormulaType.String))
                .Add(new NamedFormulaType("b", FormulaType.String));

            RecordType targetType = RecordType.Empty()
                .Add(new NamedFormulaType("a", FormulaType.Number))
                .Add(new NamedFormulaType("b", FormulaType.Boolean));

            RecordType notExpectedTargetType = RecordType.Empty()
                .Add(new NamedFormulaType("a", FormulaType.Number))
                .Add(new NamedFormulaType("b", FormulaType.Color));

            Assert.True(inputType.CanPotentiallyCoerceTo(targetType));
            Assert.True(inputType.CanPotentiallyCoerceTo(notExpectedTargetType));
        }

        [Theory]
        [InlineData("6", "False", true)]
        [InlineData("26", "true", true)]
        [InlineData("test string", "true", false)]
        [InlineData("25", "test string", false)]
        public void TryCoerceToRecordTest(string field1, string field2, bool expectedSucceeded)
        {
            var fieldName1 = "a";
            var fieldName2 = "b";
            var expectedFieldType1 = FormulaType.Number;
            var expectedFieldType2 = FormulaType.Boolean;

            RecordValue inputRecord = FormulaValue.NewRecordFromFields(
                new NamedValue(fieldName1, FormulaValue.New(field1)),
                new NamedValue(fieldName2, FormulaValue.New(field2)));

            RecordType targetType = RecordType.Empty()
                .Add(new NamedFormulaType(fieldName1, expectedFieldType1))
                .Add(new NamedFormulaType(fieldName2, expectedFieldType2));

            bool isSucceeded = inputRecord.TryCoerceTo(targetType, out RecordValue result);
            if (expectedSucceeded)
            {
                Assert.True(isSucceeded);
                Assert.Equal(FormulaValue.New(double.Parse(field1)).Value, result.GetField(fieldName1).ToObject());
                Assert.Equal(FormulaValue.New(bool.Parse(field2)).Value, result.GetField(fieldName2).ToObject());
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(result);
            }

            Assert.False(inputRecord.TryCoerceTo(RecordType.Empty(), out RecordValue res));

            RecordType notExpectedTargetType = RecordType.Empty()
                .Add(new NamedFormulaType(fieldName1, expectedFieldType1))
                .Add(new NamedFormulaType(fieldName2, FormulaType.Hyperlink));

            Assert.False(inputRecord.TryCoerceTo(notExpectedTargetType, out RecordValue nullResult));
            Assert.Null(nullResult);
        }

        [Theory]
        [InlineData("6", "False", "8", "true", true)]
        [InlineData("test", "False", "8", "true", false)]
        [InlineData("6", "False", "8", "test", false)]
        public void TryCoerceToTableTest(string field1, string field2, string field3, string field4, bool expectedSucceeded)
        {
            var fieldName1 = "a";
            var fieldName2 = "b";
            var expectedFieldType1 = FormulaType.Number;
            var expectedFieldType2 = FormulaType.Boolean;

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue(fieldName1, FormulaValue.New(field1)),
                new NamedValue(fieldName2, FormulaValue.New(field2)));

            RecordValue r2 = FormulaValue.NewRecordFromFields(
                            new NamedValue(fieldName1, FormulaValue.New(field3)),
                            new NamedValue(fieldName2, FormulaValue.New(field4)));

            TableValue tableValue = FormulaValue.NewTable(r1.Type, r1, r2);

            RecordType targetType = RecordType.Empty()
                .Add(new NamedFormulaType(fieldName1, expectedFieldType1))
                .Add(new NamedFormulaType(fieldName2, expectedFieldType2));

            bool isSucceeded = tableValue.TryCoerceTo(targetType.ToTable(), out TableValue result);

            if (expectedSucceeded)
            {
                Assert.True(isSucceeded);
                int i = 0;

                foreach (var row in result.Rows)
                {
                    if (i++ == 0)
                    {
                        Assert.Equal(FormulaValue.New(double.Parse(field1)).Value, row.Value.GetField(fieldName1).ToObject());
                        Assert.Equal(FormulaValue.New(bool.Parse(field2)).Value, row.Value.GetField(fieldName2).ToObject());
                    }
                    else
                    {
                        Assert.Equal(FormulaValue.New(double.Parse(field3)).Value, row.Value.GetField(fieldName1).ToObject());
                        Assert.Equal(FormulaValue.New(bool.Parse(field4)).Value, row.Value.GetField(fieldName2).ToObject());
                    }
                }
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(result);
            }
        }

        private void TryCoerceFromSourceTypeToTargetType(FormulaValue value, FormulaType target, bool expectedSucceeded, string expected)
        {
            bool isSucceeded = value.TryCoerceTo(target, out FormulaValue result);
            if (expectedSucceeded)
            {
                Assert.True(isSucceeded);
                Assert.Equal(expected.ToLower(), result.ToObject().ToString().ToLower());
                Assert.Equal(target, result.Type);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(result);
            }
        }

        private void TryCoerceToTargetTypes(FormulaValue inputValue, string exprBool, string exprNumber, string exprDecimal, string exprStr, string exprDateTime)
        {
            bool isSucceeded = inputValue.TryCoerceTo(out BooleanValue resultBoolean);            
            if (exprBool != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(bool.Parse(exprBool), resultBoolean.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(resultBoolean);
            }
            
            isSucceeded = inputValue.TryCoerceTo(out NumberValue resultNumber);
            if (exprNumber != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(double.Parse(exprNumber), resultNumber.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(resultNumber);
            }

            isSucceeded = inputValue.TryCoerceTo(out DecimalValue resultDecimal);
            if (exprDecimal != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(decimal.Parse(exprDecimal), resultDecimal.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(resultDecimal);
            }

            isSucceeded = inputValue.TryCoerceTo(out StringValue resultValue);
            if (exprStr != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(exprStr, resultValue.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.IsType<ErrorValue>(resultValue);
            }

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(CultureInfo.CurrentCulture);
            runtimeConfig.SetTimeZone(TimeZoneInfo.Utc);
            isSucceeded = inputValue.TryCoerceTo(runtimeConfig, out StringValue resultString);
            if (exprStr != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(exprStr, resultString.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.IsType<ErrorValue>(resultString);
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(3000);
            isSucceeded = inputValue.TryCoerceTo(runtimeConfig, cts.Token, out StringValue cResultString);
            if (exprStr != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(exprStr, cResultString.Value);
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.IsType<ErrorValue>(cResultString);
            }

            isSucceeded = inputValue.TryCoerceTo(out DateTimeValue resultDateTime);
            if (exprDateTime != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(DateTime.Parse(exprDateTime), resultDateTime.GetConvertedValue(TimeZoneInfo.Local));
            }
            else
            {
                Assert.False(isSucceeded);
                Assert.Null(resultDateTime);
            }
        }
    }
}
