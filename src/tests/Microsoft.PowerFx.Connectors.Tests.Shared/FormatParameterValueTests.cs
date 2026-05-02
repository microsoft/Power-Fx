// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    /// <summary>
    /// Tests for <see cref="HttpFunctionInvoker.FormatParameterValue"/> which ensures
    /// DateTime/Date values are formatted as ISO 8601 in query, path, and header parameters,
    /// rather than using culture-dependent DateTime.ToString().
    /// Related: ADO Bug 32023853, Power-Fx GitHub Issue #2880.
    /// </summary>
    public class FormatParameterValueTests : PowerFxTest
    {
        [Fact]
        public void DateTimeValue_WithDateTimeFormat_ReturnsIso8601Utc()
        {
            // Arrange: a UTC DateTime
            var dt = new DateTime(2025, 12, 10, 15, 2, 11, 886, DateTimeKind.Utc);
            var dtv = FormulaValue.New(dt);
            var utcConverter = new ConvertToUTC(TimeZoneInfo.Utc);

            // Act
            var result = HttpFunctionInvoker.FormatParameterValue(dtv, "date-time", utcConverter);

            // Assert: must produce ISO 8601 UTC format
            Assert.Equal("2025-12-10T15:02:11.886Z", result);
        }

        [Fact]
        public void DateTimeValue_WithNullFormat_StillReturnsIso8601()
        {
            // Even when schema format is null, DateTimeValue should still be ISO 8601
            var dt = new DateTime(2025, 7, 23, 7, 0, 0, 0, DateTimeKind.Utc);
            var dtv = FormulaValue.New(dt);
            var utcConverter = new ConvertToUTC(TimeZoneInfo.Utc);

            var result = HttpFunctionInvoker.FormatParameterValue(dtv, null, utcConverter);

            Assert.Equal("2025-07-23T07:00:00.000Z", result);
        }

        [Fact]
        public void DateTimeValue_WithDateNoTzFormat_ReturnsNoTimezone()
        {
            var dt = new DateTime(2025, 12, 10, 15, 2, 11, 886, DateTimeKind.Unspecified);
            var dtv = new DateTimeValue(IRContext.NotInSource(FormulaType.DateTimeNoTimeZone), dt);
            var utcConverter = new ConvertToUTC(TimeZoneInfo.Utc);

            var result = HttpFunctionInvoker.FormatParameterValue(dtv, "date-no-tz", utcConverter);

            Assert.Equal("2025-12-10T15:02:11.886", result);
        }

        [Fact]
        public void DateTimeValue_LocalTime_ConvertsToUtc()
        {
            // Arrange: a local time that the converter will shift to UTC
            var pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var localDt = new DateTime(2025, 12, 10, 7, 0, 0, 0, DateTimeKind.Unspecified);
            var dtv = FormulaValue.New(localDt);
            var utcConverter = new ConvertToUTC(pst);

            // Act
            var result = HttpFunctionInvoker.FormatParameterValue(dtv, "date-time", utcConverter);

            // Assert: PST is UTC-8, so 7:00 PST = 15:00 UTC
            Assert.Equal("2025-12-10T15:00:00.000Z", result);
        }

        [Fact]
        public void DateValue_ReturnsIso8601DateOnly()
        {
            var dt = new DateTime(2025, 12, 10, 0, 0, 0, DateTimeKind.Utc);
            var dv = FormulaValue.NewDateOnly(dt);

            var result = HttpFunctionInvoker.FormatParameterValue(dv, "date", null);

            Assert.Equal("2025-12-10", result);
        }

        [Fact]
        public void StringValue_PassesThrough()
        {
            var sv = FormulaValue.New("hello world");

            var result = HttpFunctionInvoker.FormatParameterValue(sv, null, null);

            Assert.Equal("hello world", result);
        }

        [Fact]
        public void NumberValue_PassesThrough()
        {
            var nv = FormulaValue.New(42.5);

            var result = HttpFunctionInvoker.FormatParameterValue(nv, null, null);

            Assert.Equal("42.5", result);
        }

        [Fact]
        public void BooleanValue_PassesThrough()
        {
            var bv = FormulaValue.New(true);

            var result = HttpFunctionInvoker.FormatParameterValue(bv, null, null);

            Assert.Equal("True", result);
        }

        [Fact]
        public void NullValue_ReturnsEmpty()
        {
            var result = HttpFunctionInvoker.FormatParameterValue(null, "date-time", null);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void BlankValue_ReturnsEmpty()
        {
            var blank = FormulaValue.NewBlank();

            var result = HttpFunctionInvoker.FormatParameterValue(blank, "date-time", null);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DateTimeValue_NullUtcConverter_FallsBackToUtcConversion()
        {
            // When utcConverter is null, should still produce valid ISO 8601 via GetConvertedValue
            var dt = new DateTime(2025, 12, 10, 15, 0, 0, 0, DateTimeKind.Utc);
            var dtv = FormulaValue.New(dt);

            var result = HttpFunctionInvoker.FormatParameterValue(dtv, "date-time", null);

            Assert.Equal("2025-12-10T15:00:00.000Z", result);
        }
    }
}
