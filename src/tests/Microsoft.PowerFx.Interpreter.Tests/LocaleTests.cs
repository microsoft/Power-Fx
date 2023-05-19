// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class LocaleTests : PowerFxTest
    {
        [Fact]
        public async Task NoCurrentCulture1Test()
        {
            // Setup
            CultureInfo culture = new CultureInfo("es-ES");
            CultureInfo.CurrentCulture = culture;

            var parserOptions = new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = true };
            RecalcEngine recalcEngine = new RecalcEngine();

            var check = recalcEngine.Check("Float(\"4,99\")", parserOptions);
            var run = check.GetEvaluator();
            var result = await run.EvalAsync(CancellationToken.None).ConfigureAwait(false);

            // Although CultureInfo.CurrentCulture has been set, Eval will run under CultureInfo.InvariantCulture.
            Assert.Equal(499D, result.ToObject());
        }
    }
}
