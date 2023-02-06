// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Marshal;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Test type coercion between C# objects and Power Fx values. 
    public class TypeCoercionTest : PowerFxTest
    {
        [Fact]
        public void TryCoerce()
        {
            TryCoerceToSuccess(null, FormulaType.String, FormulaValue.NewBlank(FormulaType.String));
            TryCoerceToFailure(new StringValue(IRContext.NotInSource(FormulaType.String), "TestLink"), FormulaType.Hyperlink);
        }

        [Fact]
        public void TryCoerceToBoolean()
        {
            // From Number to Boolean
            TryCoerceFromNumberToBooleanSuccess(0, false);
            TryCoerceFromNumberToBooleanSuccess(0.0, false);
            TryCoerceFromNumberToBooleanSuccess(1, true);
            TryCoerceFromNumberToBooleanSuccess(1.1, true);
            TryCoerceFromNumberToBooleanSuccess(12345.123, true);
        
            // From Boolean to Boolean
            TryCoerceFromBooleanToBooleanSuccess(true, true);
            TryCoerceFromBooleanToBooleanSuccess(false, false);

            // From String to Boolean
            // Expect success
            TryCoerceFromStringToBooleanSuccess("true", true);
            TryCoerceFromStringToBooleanSuccess("false", false);
            TryCoerceFromStringToBooleanSuccess("True", true);
            TryCoerceFromStringToBooleanSuccess("False", false);

            // Expect failure
            TryCoerceFromStringToBooleanFailure("1");
            TryCoerceFromStringToBooleanFailure("0");
        }

        [Fact]
        public void TryCoerceToString()
        {
            // From Number to String
            TryCoerceFromNumberToStringSuccess(5, "5");
            TryCoerceFromNumberToStringSuccess(1.2, "1.2");

            // From DateTime to String
            var dateTime = new DateTime(2023, 2, 1, 0, 0, 0, 0);
            TryCoerceFromDateTimeToStringSuccess(dateTime, dateTime.ToString("M/d/yyyy hh:mm tt"));

            // From Boolean to String
            TryCoerceFromBooleanToStringSuccess(true, "true");
            TryCoerceFromBooleanToStringSuccess(false, "false");

            // From String to String
            TryCoerceFromStringToStringSuccess("test string", "test string");
        }

        [Fact]
        public void TryCoerceToNumber()
        {
            // From DateTime to Number
            var dateTime = new DateTime(2023, 2, 1, 0, 0, 0, 0);
            TryCoerceFromDateTimeToNumberSuccess(dateTime, dateTime.ToOADate());

            // From Boolean to Number
            TryCoerceFromBooleanToNumberSuccess(true, 1);
            TryCoerceFromBooleanToNumberSuccess(false, 0);

            // From Number to Number
            TryCoerceFromNumberToNumberSuccess(12, 12);
            TryCoerceFromNumberToNumberSuccess(1.2, 1.2);

            // From String to Number
            TryCoerceFromStringToNumberSuccess("23", 23);
            TryCoerceFromStringToNumberSuccess("12.34", 12.34);

            // Expect failure
            TryCoerceFromStringToNumberFailure("test12");
        }

        [Fact]
        public void TryCoerceToDateTime()
        {
            var dateTime = new DateTime(2023, 2, 1, 0, 0, 0, 0);
                        
            // From String to DateTime
            TryCoerceFromStringToDateTimeSuccess(dateTime.ToString(), dateTime);

            // From Number to DateTime 44958
            TryCoerceFromNumberToDateTimeSuccess(dateTime.ToOADate(), dateTime);
            
            // From DateTime to DateTime
            TryCoerceFromDateTimeToDateTimeSuccess(dateTime, dateTime);

            // Expect failure
            TryCoerceFromStringToDateTimeFailure("day1month2year2023");
        }

        private void TryCoerceToSuccess(FormulaValue inputValue, FormulaType targetType, FormulaValue expectedValue)
        {
            var isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, targetType, out FormulaValue result);

            Assert.True(isSucceeded);
            Assert.Equal(targetType, result.Type);
            Assert.Equal(expectedValue.ToString(), result.ToString());
        }

        private void TryCoerceToFailure(FormulaValue inputValue, FormulaType targetType)
        {
            var isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, targetType, out FormulaValue result);

            Assert.False(isSucceeded);
        }

        private void TryCoerceFromStringToBooleanSuccess(string inputValue, bool expectedValue)
        {
            TryCoerceToSuccess(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.Boolean, new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), expectedValue));
        }

        private void TryCoerceFromStringToBooleanFailure(string inputValue)
        {
            TryCoerceToFailure(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.Boolean);
        }

        private void TryCoerceFromNumberToBooleanSuccess(double inputValue, bool expectedValue)
        {
            TryCoerceToSuccess(new NumberValue(IRContext.NotInSource(FormulaType.Number), inputValue), FormulaType.Boolean, new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), expectedValue));
        }

        private void TryCoerceFromBooleanToBooleanSuccess(bool inputValue, bool expectedValue)
        {
            TryCoerceToSuccess(new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), inputValue), FormulaType.Boolean, new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), expectedValue));
        }

        private void TryCoerceFromNumberToStringSuccess(double inputValue, string expectedValue)
        {
            TryCoerceToSuccess(new NumberValue(IRContext.NotInSource(FormulaType.Number), inputValue), FormulaType.String, new StringValue(IRContext.NotInSource(FormulaType.String), expectedValue));
        }

        private void TryCoerceFromDateTimeToStringSuccess(DateTime inputValue, string expectedValue)
        {
            TryCoerceToSuccess(new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), inputValue), FormulaType.String, new StringValue(IRContext.NotInSource(FormulaType.String), expectedValue));
        }

        private void TryCoerceFromBooleanToStringSuccess(bool inputValue, string expectedValue)
        {
            TryCoerceToSuccess(new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), inputValue), FormulaType.String, new StringValue(IRContext.NotInSource(FormulaType.String), expectedValue));
        }

        private void TryCoerceFromStringToStringSuccess(string inputValue, string expectedValue)
        {
            TryCoerceToSuccess(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.String, new StringValue(IRContext.NotInSource(FormulaType.String), expectedValue));
        }

        private void TryCoerceFromNumberToNumberSuccess(double inputValue, double expectedValue)
        {
            TryCoerceToSuccess(new NumberValue(IRContext.NotInSource(FormulaType.Number), inputValue), FormulaType.Number, new NumberValue(IRContext.NotInSource(FormulaType.Number), expectedValue));
        }

        private void TryCoerceFromDateTimeToNumberSuccess(DateTime inputValue, double expectedValue)
        {
            TryCoerceToSuccess(new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), inputValue), FormulaType.Number, new NumberValue(IRContext.NotInSource(FormulaType.Number), expectedValue));
        }

        private void TryCoerceFromBooleanToNumberSuccess(bool inputValue, double expectedValue)
        {
            TryCoerceToSuccess(new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), inputValue), FormulaType.Number, new NumberValue(IRContext.NotInSource(FormulaType.Number), expectedValue));
        }

        private void TryCoerceFromStringToNumberSuccess(string inputValue, double expectedValue)
        {
            TryCoerceToSuccess(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.Number, new NumberValue(IRContext.NotInSource(FormulaType.Number), expectedValue));
        }

        private void TryCoerceFromStringToNumberFailure(string inputValue)
        {
            TryCoerceToFailure(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.Number);
        }

        private void TryCoerceFromNumberToDateTimeSuccess(double inputValue, DateTime expectedValue)
        {
            TryCoerceToSuccess(new NumberValue(IRContext.NotInSource(FormulaType.Number), inputValue), FormulaType.DateTime, new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), expectedValue));
        }

        private void TryCoerceFromDateTimeToDateTimeSuccess(DateTime inputValue, DateTime expectedValue)
        {
            TryCoerceToSuccess(new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), inputValue), FormulaType.DateTime, new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), expectedValue));
        }

        private void TryCoerceFromStringToDateTimeSuccess(string inputValue, DateTime expectedValue)
        {
            TryCoerceToSuccess(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.DateTime, new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), expectedValue));
        }

        private void TryCoerceFromStringToDateTimeFailure(string inputValue)
        {
            TryCoerceToFailure(new StringValue(IRContext.NotInSource(FormulaType.String), inputValue), FormulaType.DateTime);
        }
    }
}
