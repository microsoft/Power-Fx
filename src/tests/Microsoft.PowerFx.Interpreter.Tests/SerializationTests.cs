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

            foreach (var dt in new[] { DateTime.Now, DateTime.UtcNow, DateTime.Parse("10/10/2022") })
            {
                if (dt.Kind == DateTimeKind.Utc)
                {
                    // UTC is not allowed
                    Assert.Throws<ArgumentException>(() => FormulaValue.New(DateTime.UtcNow));
                }
                else
                {
                    var dateTimeValue = FormulaValue.New(dt);
                    var dateTimeValueDeserialized = (DateTimeValue)engine.Eval(dateTimeValue.ToExpression());

                    Assert.Equal(dateTimeValue.Value, dateTimeValueDeserialized.Value);
                }
            }
        }
    }
}
