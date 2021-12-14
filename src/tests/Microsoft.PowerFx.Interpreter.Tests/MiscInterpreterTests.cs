using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Tests;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;
namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class MiscInterpreterTests
    {
        RecalcEngine engine = new RecalcEngine();

        [Fact]
        public void TestRand()
        {
            for (int i = 0; i < 100; i++)
            {
                var numberValue = engine.Eval("Rand()") as NumberValue;
                Assert.NotNull(numberValue);
                Assert.True(numberValue.Value < 1.0 && numberValue.Value > 0);
            }
        }

        [Fact]
        public void TestRandBetween()
        {
            for (int i = 0; i < 100; i++)
            {
                var numberValue = engine.Eval("RandBetween(1,100)") as NumberValue;
                Assert.NotNull(numberValue);
                Assert.True(numberValue.Value <= 100 && numberValue.Value >= 1);
            }
        }
    }
}
