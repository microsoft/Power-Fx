// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Tests
{
    public class LanguageTest : PowerFxTest
    {
        private static CultureInfo _defaultCulture;

        // Test language
        [Fact]
        public void GetLanguageTest()
        {
            var vnCulture = "vi-VN";
            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(new CultureInfo(vnCulture));
            runtimeConfig.SetTimeZone(TimeZoneInfo.Utc);
            var runner = new EvalVisitor(runtimeConfig, CancellationToken.None);

            var language = Language(runner, IRContext.NotInSource(FormulaType.String));
            Assert.Equal(vnCulture.ToLower(), language.Value.ToLower());
        }

        // Test default language
        [Fact]
        public void GetDefaultLanguageTest()
        {
            var engine = new RecalcEngine();
            var result = engine.Eval("Language()");

            Assert.Equal("en-US", result.ToObject());
        }

        [Fact]
        public void GetLanguageForNullCulture()
        {
            var runtimeConfig = new RuntimeConfig();
            var runner = new EvalVisitor(runtimeConfig, CancellationToken.None);

            var language = Language(runner, IRContext.NotInSource(FormulaType.String));
            Assert.Equal("en-US", language.Value);

            _defaultCulture = null;
            TestDefaultCulture(null);
        }

        [Fact]
        public void GetLanguageForInvariantCulture()
        {
            _defaultCulture = CultureInfo.InvariantCulture;
            ConfigTests.RunOnIsolatedThread(_defaultCulture, TestDefaultCulture);
        }

        [Theory]
        [InlineData("1234,5678", "#.##0,00", "1.234,57", "vi-VN")]
        [InlineData("1234,5678", "#.##0,00", "1.234,57", "pt-BR")]
        [InlineData("1234,5678", "# ##0,00", "1 234,57", "fr-FR")]
        public void TextWithLanguageTest(string value, string format, string expectedResult,  string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            double.TryParse(value, NumberStyles.Float, culture, out double numValue);
            NumberValue numberInput = new NumberValue(IRContext.NotInSource(FormulaType.Number), numValue);
            StringValue formatString = new StringValue(IRContext.NotInSource(FormulaType.String), format); 

            var formatInfo = new FormattingInfo()
            {
                CultureInfo = culture,
                CancellationToken = CancellationToken.None,
                TimeZoneInfo = TimeZoneInfo.Utc
            };

            FormulaValue[] args = { numberInput, formatString };

            var result = Text(formatInfo, IRContext.NotInSource(FormulaType.String), args);

            Assert.Equal(expectedResult, (result as StringValue).Value); 
        }

        private void TestDefaultCulture(CultureInfo culture)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var result = engine.Eval("Language()", options: new ParserOptions(culture));

            Assert.Equal("en-US", result.ToObject());
        }
    }
}
