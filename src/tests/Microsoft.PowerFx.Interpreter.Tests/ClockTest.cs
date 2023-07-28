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
    public class ClockTest : PowerFxTest
    {
        // Test default clock settings
        [Fact]
        public void DefaultClockIsClock24Test()
        {
            var engine = new RecalcEngine();
            var result = engine.Eval("Clock.IsClock24()");

            Assert.Equal(false, result.ToObject());
        }

        [Theory]
        [InlineData("en-US", false)]
        [InlineData("pt-BR", true)]
        [InlineData("fr-FR", true)]
        [InlineData("fi-FI", true)]
        public void TestClockIsClock24(string cultureName, bool isClock24)
        {
            var culture = new CultureInfo(cultureName);
            var recalcEngine = new RecalcEngine(new PowerFxConfig(Features.PowerFxV1));
            var symbols = new RuntimeConfig();
            symbols.SetCulture(culture);

            var result = recalcEngine.EvalAsync("Clock.IsClock24()", CancellationToken.None, runtimeConfig: symbols).Result;

            Assert.Equal(isClock24, (result as BooleanValue).Value);
        }
    }
}
