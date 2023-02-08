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
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Marshal;
using Microsoft.PowerFx.Interpreter.Tests;
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
        [InlineData(1, "true", "1", "1", "12/31/1899 12:00 AM")]
        [InlineData(0, "false", "0", "0", "12/30/1899 12:00 AM")]
        [InlineData(44962, "true", "44962", "44962", "2/5/2023 12:00 AM")]
        public void TryCoerceFromNumberTest(double value, string exprBool, string exprNumber, string exprStr, string exprDateTime)
        {
            TryCoerceToTargetTypes(FormulaValue.New(value), exprBool, exprNumber, exprStr, exprDateTime);
        }

        // From string to other types
        [Theory]
        [InlineData("", null, null, "", null)]
        [InlineData("1", null, "1", "1", null)]
        [InlineData("true", "true", null, "true", null)]
        [InlineData("false", "false", null, "false", null)]
        [InlineData("This is a string", null, null, "This is a string", null)]
        public void TryCoerceFromStringTest(string value, string exprBool, string exprNumber, string exprStr, string exprDateTime)
        {
            TryCoerceToTargetTypes(FormulaValue.New(value), exprBool, exprNumber, exprStr, exprDateTime);
        }

        // From boolean to other types
        [Theory]
        [InlineData(true, "true", "1", "true", null)]
        [InlineData(false, "false", "0", "false", null)]
        public void TryCoerceFromBooleanTest(bool value, string exprBool, string exprNumber, string exprStr, string exprDateTime)
        {
            TryCoerceToTargetTypes(FormulaValue.New(value), exprBool, exprNumber, exprStr, exprDateTime);
        }

        // From dateTime to other types
        [Theory]
        [InlineData("2/5/2023", null, "44962", "2/5/2023 12:00 AM", "2/5/2023 12:00 AM")]
        public void TryCoerceFromDateTimeTest(string value, string exprBool, string exprNumber, string exprStr, string exprDateTime)
        {
            TryCoerceToTargetTypes(FormulaValue.New(DateTime.Parse(value)), exprBool, exprNumber, exprStr, exprDateTime);
        }

        private void TryCoerceToTargetTypes(FormulaValue inputValue, string exprBool, string exprNumber, string exprStr, string exprDateTime)
        {
            bool isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, out BooleanValue resultBoolean);
            if (exprBool != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(bool.Parse(exprBool), resultBoolean.Value);
            }
            else
            {
                Assert.False(isSucceeded);
            }

            isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, out NumberValue resultNumber);
            if (exprNumber != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(double.Parse(exprNumber), resultNumber.Value);
            }
            else
            {
                Assert.False(isSucceeded);
            }

            isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, out StringValue resultString);
            if (exprStr != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(exprStr, resultString.Value);
            }
            else
            {
                Assert.False(isSucceeded);
            }

            isSucceeded = TypeCoercionProvider.TryCoerceTo(inputValue, out DateTimeValue resultDateTime);
            if (exprDateTime != null)
            {
                Assert.True(isSucceeded);
                Assert.Equal(DateTime.Parse(exprDateTime), resultDateTime.GetConvertedValue(TimeZoneInfo.Local));
            }
            else
            {
                Assert.False(isSucceeded);
            }
        }
    }
}
