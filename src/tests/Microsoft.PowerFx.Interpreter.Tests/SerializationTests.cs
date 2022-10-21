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

                    // !JYL! Comparing this way since PFx doesnt consider tick value
                    var datetime1 = dateTimeValue.Value;
                    var datetime2 = dateTimeValueDeserialized.Value;

                    var timeSpan1 = datetime1.TimeOfDay;
                    var timeSpan2 = datetime2.TimeOfDay;

                    var equal = datetime1.Year == datetime2.Year && 
                                datetime1.Month == datetime2.Month && 
                                datetime1.Day == datetime2.Day;

                    equal = equal && 
                            timeSpan1.Hours == timeSpan2.Hours && 
                            timeSpan1.Minutes == timeSpan2.Minutes && 
                            timeSpan1.Seconds == timeSpan2.Seconds && 
                            timeSpan1.Milliseconds == timeSpan2.Milliseconds;

                    Assert.True(equal);
                }
            }            
        }
    }
}
