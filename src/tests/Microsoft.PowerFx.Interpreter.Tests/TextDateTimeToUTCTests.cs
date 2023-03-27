// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TextDateTimeToUTCTests : PowerFxTest
    {
        [Theory]
        [InlineData("Text(DateTimeValue(\"March 10, 2023 5:30 PM\"), DateTimeFormat.UTC)", "Pacific Standard Time", "2023-03-11T01:30:00.000Z")]
        [InlineData("Text(DateTimeValue(\"March 10, 2023 5:30 PM\"), DateTimeFormat.UTC)", "Tokyo Standard Time", "2023-03-10T08:30:00.000Z")]
        [InlineData("Text(DateTimeValue(\"March 10, 2023 5:30 PM\"), DateTimeFormat.UTC)", "SE Asia Standard Time", "2023-03-10T10:30:00.000Z")]
        public async void TextDateTimeToUTC(string inputExp, string timeZoneId, string expectedDateTimeUTC)
        {
            var engine = new RecalcEngine();
            var rc = new RuntimeConfig();
            rc.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

            var check = engine.Check(inputExp);
            Assert.True(check.IsSuccess);

            var utcResult = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            Assert.Equal(expectedDateTimeUTC, utcResult.ToObject());
        }
    }
}
