// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DateValueLocaleTests
    {
        [Theory]
        [InlineData("bg-BG")]
        [InlineData("ca-ES")]
        [InlineData("cs-CZ")]
        [InlineData("da-DK")]
        [InlineData("de-DE")]
        [InlineData("el-GR")]
        [InlineData("en-US")]
        [InlineData("es-ES")]
        [InlineData("et-EE")]
        [InlineData("fi-FI")]
        [InlineData("fr-FR")]
        [InlineData("gl-ES")]
        [InlineData("hi-IN")]
        [InlineData("hr-HR")]
        [InlineData("hu-HU")]
        [InlineData("id-ID")]
        [InlineData("it-IT")]
        [InlineData("ja-JP")]
        [InlineData("kk-KZ")]
        [InlineData("ko-KR")]
        [InlineData("lt-LT")]
        [InlineData("lv-LV")]
        [InlineData("ms-MY")]
        [InlineData("nb-NO")]
        [InlineData("nl-NL")]
        [InlineData("pl-PL")]
        [InlineData("pt-BR")]
        [InlineData("pt-PT")]
        [InlineData("ro-RO")]
        [InlineData("ru-RU")]
        [InlineData("sk-SK")]
        [InlineData("sl-SI")]
        [InlineData("sr-Cyrl-RS")]
        [InlineData("sr-Latn-RS")]
        [InlineData("sv-SE")]
        [InlineData("th-TH")]
        [InlineData("tr-TR")]
        [InlineData("uk-UA")]
        [InlineData("vi-VN")]
        [InlineData("zh-CN")]
        [InlineData("zh-TW")]

        //[InlineData("eu-ES")] PFx and PA are unable to parse long date format back to date.
        public void DateValueLocaleTest(string locale)
        {
            DateTime datetime = new DateTime(2021, 4, 9);

            var expressionShort = $"Text(Date(2021, 4, 9), DateTimeFormat.ShortDate, \"{locale}\")";
            var expressionLong = $"Text(Date(2021, 4, 9), DateTimeFormat.LongDate, \"{locale}\")";

            var engine = new RecalcEngine();

            var resultShort = (StringValue)engine.Eval(expressionShort);
            var resultLong = (StringValue)engine.Eval(expressionLong);

            var reversedShort = (DateValue)engine.Eval($"DateValue(\"{resultShort.Value}\", \"{locale}\")");
            Assert.Equal(datetime, reversedShort.GetConvertedValue(null));

            var reversedLong = (DateValue)engine.Eval($"DateValue(\"{resultLong.Value}\", \"{locale}\")");
            Assert.Equal(datetime, reversedLong.GetConvertedValue(null));
        }
    }
}
