// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TimeTests : PowerFxTest
    {
        public TimeTests()
            : base()
        {
            _engine = new RecalcEngine();
        }

        private readonly RecalcEngine _engine;

        [Fact]
        public void TestTimeZoneOffsetNonDST()
        {
            var tzInfo = TimeZoneInfo.Local;
            var testDate = new DateTime(2021, 6, 1);
            var tzOffsetDays = tzInfo.GetUtcOffset(testDate).TotalDays * -1440;
            var numberValue = _engine.Eval("TimeZoneOffset(Date(2021, 6, 1))") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestTimeZoneOffsetDST()
        {
            var tzInfo = TimeZoneInfo.Local;
            var testDate = new DateTime(2021, 12, 1);
            var tzOffsetDays = tzInfo.GetUtcOffset(testDate).TotalDays * -1440;
            var numberValue = _engine.Eval("TimeZoneOffset(Date(2021, 12, 1))") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestCurrentTimeZoneOffset()
        {
            var tzInfo = TimeZoneInfo.Local;
            var tzOffsetDays = tzInfo.GetUtcOffset(DateTime.Now).TotalDays * -1440;
            var numberValue = _engine.Eval("TimeZoneOffset()") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestConvertToUTC()
        {
            var testTime = DateTime.Now;
            var result = _engine.Eval($"DateAdd(DateTimeValue(\"{testTime.ToString()}\"), TimeZoneOffset(), TimeUnit.Minutes)");
            Assert.NotNull(result);
            if (result is DateTimeValue dateResult)
            {
                var dateResultValue = dateResult.GetConvertedValue(null);
                Assert.Equal(dateResultValue.Date, testTime.ToUniversalTime().Date);
                Assert.Equal(dateResultValue.TimeOfDay.Hours, testTime.ToUniversalTime().TimeOfDay.Hours);
                Assert.Equal(dateResultValue.TimeOfDay.Minutes, testTime.ToUniversalTime().TimeOfDay.Minutes);
                Assert.Equal(dateResultValue.TimeOfDay.Seconds, testTime.ToUniversalTime().TimeOfDay.Seconds);
            }
            else
            {
                Assert.Fail("result was not a DateTimeValue");
            }
        }

        [Fact]
        public void TestConvertFromUTC()
        {
            var testTime = DateTime.UtcNow.AddHours(-1);
            var result = _engine.Eval($"DateTimeValue(\"{testTime.ToString()}\") + -TimeZoneOffset()/60/24");
            Assert.NotNull(result);
            if (result is DateTimeValue dateResult)
            {
                var dateResultValue = dateResult.GetConvertedValue(null);
                Assert.Equal(dateResultValue.Date, testTime.ToLocalTime().Date);
                Assert.Equal(dateResultValue.TimeOfDay.Hours, testTime.ToLocalTime().TimeOfDay.Hours);
                Assert.Equal(dateResultValue.TimeOfDay.Minutes, testTime.ToLocalTime().TimeOfDay.Minutes);
                Assert.Equal(dateResultValue.TimeOfDay.Seconds, testTime.ToLocalTime().TimeOfDay.Seconds);
            }
            else
            {
                Assert.Fail("result was not a DateTimeValue");
            }
        }

        public class TestClockService : IClockService
        {
            public DateTime UtcNow { get; set; } = new DateTime(2023, 6, 2, 3, 15, 7, DateTimeKind.Utc);
        }

        // Test Now/Today expressions.
        // Use explicit clock service to set time and be fully deterministic. 
        [Theory]
        [InlineData("Today()", "6/1/2023 12:00:00 AM")]
        [InlineData("Now()", "6/1/2023 8:15:07 PM")]
        [InlineData("IsToday(Now())", "True")]
        [InlineData("IsToday(Date(2023, 6,2))", "False")] // converts to local
        [InlineData("IsToday(Date(2023, 6,1))", "True")]
        [InlineData("Day(Now())", "1")]
        [InlineData("TimeZoneOffset(Date(2023,10,1))", "420")]
        [InlineData("TimeZoneOffset(Date(2023,11,6))", "480")]
        public async Task TestWithClockService(string expr, string expectedResultStr)
        {
            RuntimeConfig rc = new RuntimeConfig();
            rc.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            rc.SetClock(new TestClockService());

            var result = await _engine.EvalAsync(expr, default, runtimeConfig: rc);

            var actualResultStr = result.ToObject().ToString();

            Assert.Equal(expectedResultStr, actualResultStr);
        }

        // Test Now/Today expressions.
        // Use explicit clock service to set time and be fully deterministic. 
        [Fact]
        public async Task TestTimeZoneInfoWithClockService()
        {
            RuntimeConfig rc = new RuntimeConfig();
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            rc.SetTimeZone(tzi);
            rc.SetClock(new TestClockService());

            var result = await _engine.EvalAsync("TimeZoneOffset()", default, runtimeConfig: rc);
            var currentOffset = -tzi.GetUtcOffset(DateTime.Now).TotalMinutes;

            var actualResultStr = result.ToObject().ToString();

            Assert.Equal(currentOffset.ToString(), actualResultStr);
        }
    }
}
