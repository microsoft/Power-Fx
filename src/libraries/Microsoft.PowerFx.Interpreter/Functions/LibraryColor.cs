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
        private static readonly Regex RegexColorTable = new Regex(@"^#(?<a>[0-9a-fA-F]{2})(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})?$", LibraryFlags.RegExFlags);

        // CSS format is RGBA
        private static readonly Regex RegexCSS = new Regex(@"^#(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})(?<a>[0-9a-fA-F]{2})?$", LibraryFlags.RegExFlags);

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
            var red = (int)args[0].Value;
            var green = (int)args[1].Value;
            var blue = (int)args[2].Value;
            var alpha = args[3].Value;

            // Ensure rgb numbers are in range (0-255)
            if (red < 0 || red > 255
                || green < 0 || green > 255
                || blue < 0 || blue > 255)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            // Ensure alpha is between 0 and 1
            if (alpha < 0.0d || alpha > 1.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var alpha255 = System.Convert.ToInt32(alpha * 255.0d);

            return new ColorValue(irContext, Color.FromArgb(alpha255, red, green, blue));
        }

        public static FormulaValue ColorFade(IRContext irContext, FormulaValue[] args)
        {
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
