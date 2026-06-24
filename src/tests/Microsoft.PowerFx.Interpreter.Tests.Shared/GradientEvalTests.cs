// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class GradientEvalTests : PowerFxTest
    {
        [Fact]
        public void LinearGradient_Evaluates_ToGradientValue()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var result = engine.Eval("LinearGradient(RGBA(255,0,0,1), RGBA(0,0,255,1), 90)");

            var g = Assert.IsType<GradientValue>(result);
            Assert.Equal(90, g.Angle);
            Assert.Equal(2, g.Stops.Count);
            Assert.Equal(255, g.Stops[0].Color.R);
            Assert.Equal(255, g.Stops[1].Color.B);
        }

        [Fact]
        public void Color_Coerces_ToSingleStopGradient_AtRuntime()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            // Force ColorToGradient coercion: the false-branch is a Color (RGBA) while
            // the true-branch is a Gradient. The binder uses the first branch type (Gradient)
            // as the result type and inserts UnaryOpKind.ColorToGradient on the Color branch.
            // The condition is false (1=0) so the Color branch executes at runtime.
            var result = engine.Eval(
                "If(1=0, LinearGradient(RGBA(0,0,0,1), RGBA(0,0,0,1), 0), RGBA(255,0,0,1))");

            var g = Assert.IsType<GradientValue>(result);
            Assert.True(g.Stops.Count >= 1);
            Assert.Equal(255, g.Stops[0].Color.R);
        }

        [Fact]
        public void Gradient_CoercesTo_Color_FirstStop()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var gradResult = engine.Eval("ColorFade(LinearGradient(RGBA(255,0,0,1), RGBA(0,0,255,1), 90), 0)");
            var color = Assert.IsType<ColorValue>(gradResult);

            // First stop is red; ColorFade(.., 0) is identity, so the flattened color is red.
            Assert.Equal(System.Drawing.Color.FromArgb(255, 255, 0, 0), color.Value);
        }
    }
}