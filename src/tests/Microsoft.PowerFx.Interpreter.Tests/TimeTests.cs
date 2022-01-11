using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TimeTests
    {
        readonly RecalcEngine engine = new RecalcEngine();

        [Fact]
        public void TestTimeZoneOffsetNonDST()
        {
            var tzInfo = TimeZoneInfo.Local;
            var testDate = new DateTime(2021, 6, 1);
            var tzOffsetDays = tzInfo.GetUtcOffset(testDate).TotalDays * -1;
            var numberValue = engine.Eval("TimeZoneOffset(Date(2021, 6, 1))") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestTimeZoneOffsetDST()
        {
            var tzInfo = TimeZoneInfo.Local;
            var testDate = new DateTime(2021, 12, 1);
            var tzOffsetDays = tzInfo.GetUtcOffset(testDate).TotalDays * -1;
            var numberValue = engine.Eval("TimeZoneOffset(Date(2021, 12, 1))") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestCurrentTimeZoneOffset()
        {
            var tzInfo = TimeZoneInfo.Local;
            var tzOffsetDays = tzInfo.GetUtcOffset(DateTime.Now).TotalDays * -1;
            var numberValue = engine.Eval("TimeZoneOffset()") as NumberValue;
            Assert.NotNull(numberValue);
            Assert.Equal(numberValue.Value, tzOffsetDays);
        }

        [Fact]
        public void TestConvertToUTC()
        {
            var testTime = DateTime.Now;
            var result = engine.Eval($"DateTimeValue(\"{testTime.ToString()}\") + TimeZoneOffset()");
            Assert.NotNull(result);
            if (result is DateTimeValue dateResult)
            {
                Assert.Equal(dateResult.Value.Date, testTime.ToUniversalTime().Date);
                Assert.Equal(dateResult.Value.TimeOfDay.Hours, testTime.ToUniversalTime().TimeOfDay.Hours);
                Assert.Equal(dateResult.Value.TimeOfDay.Minutes, testTime.ToUniversalTime().TimeOfDay.Minutes);
                Assert.Equal(dateResult.Value.TimeOfDay.Seconds, testTime.ToUniversalTime().TimeOfDay.Seconds);
            }
            else
            {
                Assert.True(false, "result was not a DateTimeValue");
            }
        }

        [Fact]
        public void TestConvertFromUTC()
        {
            var testTime = DateTime.UtcNow.AddHours(-1);
            var result = engine.Eval($"DateTimeValue(\"{testTime.ToString()}\") + -TimeZoneOffset()");
            Assert.NotNull(result);
            if (result is DateTimeValue dateResult)
            {
                Assert.Equal(dateResult.Value.Date, testTime.ToLocalTime().Date);
                Assert.Equal(dateResult.Value.TimeOfDay.Hours, testTime.ToLocalTime().TimeOfDay.Hours);
                Assert.Equal(dateResult.Value.TimeOfDay.Minutes, testTime.ToLocalTime().TimeOfDay.Minutes);
                Assert.Equal(dateResult.Value.TimeOfDay.Seconds, testTime.ToLocalTime().TimeOfDay.Seconds);
            }
            else
            {
                Assert.True(false, "result was not a DateTimeValue");
            }
        }

    }
}
