// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // ColorTable is ARGB
        private static readonly Regex RegexColorTable = new (@"^#(?<a>[0-9a-fA-F]{2})(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})?$", RegexOptions.Compiled);

        // CSS format is RGBA
        private static readonly Regex RegexCSS = new (@"^#(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})(?<a>[0-9a-fA-F]{2})?$", RegexOptions.Compiled);

        public static FormulaValue ColorValue(IRContext irContext, StringValue[] args)
        {
            var val = args[0].Value;

            if (string.IsNullOrEmpty(val))
            {
                return new BlankValue(irContext);
            }

            Match match;
            if (ColorTable.InvariantNameToHexMap.ContainsKey(val))
            {
                var hexStringColor = string.Format("#{0:X8}", ColorTable.InvariantNameToHexMap[val]);
                match = RegexColorTable.Match(hexStringColor);
            }
            else
            {
                match = RegexCSS.Match(val);
            }

            if (match.Success)
            {
                var r = ParseColor(match, "r");
                var g = ParseColor(match, "g");
                var b = ParseColor(match, "b");
                var a = match.Groups["a"].Captures.Count == 1 ? ParseColor(match, "a") : 255;

                return new ColorValue(irContext, Color.FromArgb(a, r, g, b));
            }

            return CommonErrors.InvalidColorFormatError(irContext);
        }

        private static byte ParseColor(Match match, string color)
        {
            return byte.Parse(match.Groups[color].Value, System.Globalization.NumberStyles.HexNumber);
        }

        public static FormulaValue RGBA(IRContext irContext, NumberValue[] args)
        {
            // Ensure rgb numbers are in range (0-255)
            if (args[0].Value < 0.0d || args[0].Value > 255.0d
                || args[1].Value < 0.0d || args[1].Value > 255.0d
                || args[2].Value < 0.0d || args[2].Value > 255.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            // Truncate (strip decimals)
            var r = (int)args[0].Value;
            var g = (int)args[1].Value;
            var b = (int)args[2].Value;

            // Ensure alpha is between 0 and 1
            if (args[3].Value < 0.0d || args[3].Value > 1.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var a = System.Convert.ToInt32(args[3].Value * 255.0d);

            return new ColorValue(irContext, Color.FromArgb(a, r, g, b));
        }

        public static FormulaValue ColorFade(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                args[0] = FormulaValue.New(Color.FromArgb(0, 0, 0, 0));
            }

            if (args[1] is BlankValue)
            {
                args[1] = FormulaValue.New(0);
            }

            var color = (ColorValue)args[0];
            var fadeDelta = ((NumberValue)args[1]).Value;

            // Ensure fade amount is between -1 and 1
            if (fadeDelta < -1.0d || fadeDelta > 1.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var interpolator = Math.Abs(fadeDelta);
            var inverseInterpolator = 1 - interpolator;
            double targetComponent = fadeDelta < 0x00 ? 0x00 : 0xFF;
            targetComponent *= interpolator;
            var fadedRed = (int)Math.Floor((color.Value.R * inverseInterpolator) + targetComponent);
            var fadedGreen = (int)Math.Floor((color.Value.G * inverseInterpolator) + targetComponent);
            var fadedBlue = (int)Math.Floor((color.Value.B * inverseInterpolator) + targetComponent);
            return new ColorValue(irContext, Color.FromArgb(color.Value.A, fadedRed, fadedGreen, fadedBlue));
        }
    }
}
