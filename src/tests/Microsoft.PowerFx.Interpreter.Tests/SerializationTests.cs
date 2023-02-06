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

            DateTime[] dateTimeArray = new[]
            {
                WithoutSubMilliseconds(DateTime.Now),
                WithoutSubMilliseconds(DateTime.UtcNow),
                DateTime.Parse("10/10/2022")
            };

            foreach (var dt in dateTimeArray)
            {
                var dateTimeValue = FormulaValue.New(dt);
                var dateTimeValueDeserialized = (DateTimeValue)engine.Eval(dateTimeValue.ToExpression());

                Assert.Equal(dateTimeValue.GetConvertedValue(null), dateTimeValueDeserialized.GetConvertedValue(null));
            }
        }

        /// <summary>
        /// This is necessary due to the fact that serialization ignores tick precision.
        /// https://github.com/microsoft/Power-Fx/issues/849.
        /// </summary>
        private static DateTime WithoutSubMilliseconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);
        }
    }
}
