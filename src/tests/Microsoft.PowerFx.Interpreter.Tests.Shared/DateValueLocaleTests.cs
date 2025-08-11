// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
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
        [InlineData("eu-ES")]
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
        public void DateValueLocaleTest(string locale)
        {
            (string resultShort, string resultLong) = GetDateValueLocale(locale);

            var engine = new RecalcEngine();            

            var reversedShort = (DateValue)engine.Eval($"DateValue(\"{resultShort}\", \"{locale}\")");
            Assert.Equal(_datetime, reversedShort.GetConvertedValue(null));

            FormulaValue fv = engine.Eval($"DateValue(\"{resultLong}\", \"{locale}\")");
            DateValue reversedLong = fv as DateValue
                ?? (fv is ErrorValue ev
                    ? throw new Exception($"Locale {locale}: '{resultLong}' - {string.Join(", ", ev.Errors.Select(er => er.Message))}")
                    : throw new Exception($"DateValue returns unexpected {fv.GetType().Name} type"));

            Assert.Equal(_datetime, reversedLong.GetConvertedValue(null));
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
