// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SerializationTests : PowerFxTest
    {
        [Fact]
        public void DateTimeSerializationTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            // Local datetime
            var now = DateTime.Now;
            var dateTimeNowValue = FormulaValue.New(now);
            var dateTimeNowValueDeserialized = (DateTimeValue)engine.Eval(dateTimeNowValue.ToExpression());

            Assert.Equal(dateTimeNowValue.Value, dateTimeNowValueDeserialized.Value);

            // Unspecified
            var unspecified = DateTime.Parse("10/10/2022");
            var dateTimeUnspecifiedValue = FormulaValue.New(unspecified);
            var dateTimeUnspecifiedValueDeserialized = (DateTimeValue)engine.Eval(dateTimeUnspecifiedValue.ToExpression());

            Assert.Equal(dateTimeUnspecifiedValue.Value, dateTimeUnspecifiedValueDeserialized.Value);

            // UTC is not allowed
            Assert.Throws<ArgumentException>(() => FormulaValue.New(DateTime.UtcNow));
        }            
    }
}
