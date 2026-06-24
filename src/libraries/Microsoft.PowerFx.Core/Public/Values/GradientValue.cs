// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public readonly struct GradientStop
    {
        public Color Color { get; }

        public double Position { get; }

        public GradientStop(Color color, double position)
        {
            Color = color;
            Position = position;
        }
    }

    public class GradientValue : ValidFormulaValue
    {
        public double Angle { get; }

        public IReadOnlyList<GradientStop> Stops { get; }

        internal GradientValue(IRContext irContext, double angle, IReadOnlyList<GradientStop> stops)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Gradient);
            Contract.Assert(stops != null && stops.Count >= 1);

            Angle = angle;
            Stops = stops;
        }

        public static GradientValue NewLinear(Color start, Color end, double angle)
        {
            var stops = new[] { new GradientStop(start, 0.0), new GradientStop(end, 1.0) };
            return new GradientValue(IRContext.NotInSource(FormulaType.Gradient), angle, stops);
        }

        public override object ToObject() => this;

        public override void Visit(IValueVisitor visitor) => visitor.Visit(this);

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            string Rgba(Color c) =>
                $"RGBA({c.R},{c.G},{c.B},{Math.Round(c.A / 255.0, 3)})";

            sb.Append($"LinearGradient({Rgba(Stops[0].Color)}, {Rgba(Stops[Stops.Count - 1].Color)}, {Angle})");
        }
    }
}