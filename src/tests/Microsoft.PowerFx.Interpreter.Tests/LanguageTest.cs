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
        [InlineData("en-US", @"Text(1234.5678, ""#,##0.00"")", "1,234.57")]
        [InlineData("vi-VN", @"Text(1234.5678, ""#.##0,00"")", "1.234,57")]
        [InlineData("fr-FR", "Text(1234.5678, \"#\u202f##0,00\")", "1\u202F234,57")]
        [InlineData("fi-FI", "Text(1234.5678, \"#\u00A0##0,00\")", "1\u00A0234,57")]
        public void TextWithLanguageTest(string cultureName, string exp, string expectedResult)
        {
            var culture = new CultureInfo(cultureName);
            var recalcEngine = new RecalcEngine(new PowerFxConfig(Features.None));
            var symbols = new RuntimeConfig();
            symbols.SetCulture(culture);

            var result = recalcEngine.EvalAsync(exp, CancellationToken.None, runtimeConfig: symbols).Result;

            Assert.Equal(expectedResult, (result as StringValue).Value);
        }

        private void TestDefaultCulture(CultureInfo culture)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var result = engine.Eval("Language()", options: new ParserOptions(culture));

            Assert.Equal("en-US", result.ToObject());
        }


        [Fact]
        public void TestTextInFrench()
        {
            var parserOptions = new ParserOptions(new CultureInfo("fr-FR"));

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(new CultureInfo("fr-FR"));

            var engine = new RecalcEngine(new PowerFxConfig());
            FormulaValue result = engine.EvalAsync("Text(5/2)", CancellationToken.None, options: parserOptions, runtimeConfig: runtimeConfig).ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.IsNotType<ErrorValue>(result);
        }
    }
}
