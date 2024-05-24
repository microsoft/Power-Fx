// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DateValueLocaleTests
    {
        private readonly DateTime _datetime = new DateTime(2021, 4, 9);

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
            (string resultShort, string resultLong) = GetDateValueLocale(locale);

            var engine = new RecalcEngine();            

            var reversedShort = (DateValue)engine.Eval($"DateValue(\"{resultShort}\", \"{locale}\")");
            Assert.Equal(_datetime, reversedShort.GetConvertedValue(null));

            var reversedLong = (DateValue)engine.Eval($"DateValue(\"{resultLong}\", \"{locale}\")");
            Assert.Equal(_datetime, reversedLong.GetConvertedValue(null));
        }

        [Theory]
        [InlineData("eu-ES")] // PFx and PA are unable to parse long date format back to date.
        public void DateValueLocaleUnableToParseTest(string locale)
        {
            (_, string resultLong) = GetDateValueLocale(locale);

            var engine = new RecalcEngine();

            var reversedLong = engine.Eval($"DateValue(\"{resultLong}\", \"{locale}\")");
            Assert.IsType<ErrorValue>(reversedLong);
        }

        private (string resultShort, string resultLong) GetDateValueLocale(string locale)
        {
            var expressionShort = $"Text(Date(2021, 4, 9), DateTimeFormat.ShortDate, \"{locale}\")";
            var expressionLong = $"Text(Date(2021, 4, 9), DateTimeFormat.LongDate, \"{locale}\")";

            var engine = new RecalcEngine();

            var resultShort = (StringValue)engine.Eval(expressionShort);
            var resultLong = (StringValue)engine.Eval(expressionLong);

            return (resultShort.Value, resultLong.Value);
        }
    }
}
