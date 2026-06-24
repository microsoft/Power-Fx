// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Drawing;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LinearGradientBindingTests : PowerFxTest
    {
        [Fact]
        public void LinearGradient_Binds_To_Gradient()
        {
            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("LinearGradient(RGBA(255,0,0,1), RGBA(0,0,255,1), 90)");
            Assert.True(check.IsSuccess);
            Assert.True(check.ReturnType is GradientType);
        }

        [Fact]
        public void LinearGradient_AcceptsColorArgs_AndCoercesGradientArgToFirstStop()
        {
            // With two-way Color <-> Gradient coercion, a Gradient passed where a Color is
            // expected coerces to its first stop. So a nested LinearGradient argument now
            // binds (the inner gradient flattens to its first-stop color) and the outer call
            // still returns a Gradient, rather than being rejected at bind time.
            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("LinearGradient(LinearGradient(RGBA(0,0,0,1),RGBA(1,1,1,1),0), RGBA(0,0,255,1), 90)");
            Assert.True(check.IsSuccess);
            Assert.True(check.ReturnType is GradientType);
        }
    }

    public class GradientValueTests : PowerFxTest
    {
        [Fact]
        public void GradientValue_TwoStop_ToExpression()
        {
            var start = Color.FromArgb(255, 255, 0, 0);
            var end = Color.FromArgb(255, 0, 0, 255);
            var g = GradientValue.NewLinear(start, end, 90);

            Assert.Equal(FormulaType.Gradient, g.Type);
            Assert.Equal(2, g.Stops.Count);
            Assert.Equal(90, g.Angle);

            var sb = new StringBuilder();
            g.ToExpression(sb, new FormulaValueSerializerSettings());
            var expr = sb.ToString();

            Assert.StartsWith("LinearGradient(", expr);
            Assert.Contains("255,0,0", expr);
            Assert.Contains("0,0,255", expr);
            Assert.Contains("90", expr);
        }

        [Fact]
        public void GradientValue_Type_IsGradient()
        {
            var g = GradientValue.NewLinear(Color.Red, Color.Blue, 45);
            Assert.Equal(FormulaType.Gradient, g.Type);
        }

        [Fact]
        public void GradientValue_ToObject_ReturnsSelf()
        {
            var g = GradientValue.NewLinear(Color.Red, Color.Blue, 0);
            Assert.Same(g, g.ToObject());
        }

        [Fact]
        public void GradientStop_Properties()
        {
            var stop = new GradientStop(Color.FromArgb(255, 128, 64, 32), 0.5);
            Assert.Equal(0.5, stop.Position);
            Assert.Equal(128, stop.Color.R);
            Assert.Equal(64, stop.Color.G);
            Assert.Equal(32, stop.Color.B);
        }

        [Fact]
        public void Gradient_IsUnsupportedBy_JsonFunction()
        {
            // Gradient has no canonical JSON representation, so JSON() must reject it
            // at bind time (like OptionSet/PenImage). This keeps the JSON visitor's
            // Visit(GradientValue) defensive throw unreachable in practice.
            Assert.True(JsonFunction.HasUnsupportedType(DType.Gradient, supportsLazyTypes: false, out _, out _));
        }

        [Fact]
        public void GradientValue_ToExpression_UsesInvariantCulture()
        {
            // Under a comma-decimal culture a fractional angle must still serialize with a
            // '.' separator, not the ',' that Power Fx would parse as an extra argument.
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var g = GradientValue.NewLinear(Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 0, 255), 90.5);

                var sb = new StringBuilder();
                g.ToExpression(sb, new FormulaValueSerializerSettings());
                var expr = sb.ToString();

                Assert.Equal("LinearGradient(RGBA(255,0,0,1), RGBA(0,0,255,1), 90.5)", expr);
                Assert.DoesNotContain("90,5", expr);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}