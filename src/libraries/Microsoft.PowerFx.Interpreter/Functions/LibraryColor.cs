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
    internal partial class Library
    {
        public static FormulaValue TextToColor(IRContext irContext, StringValue[] args)
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

                // ColorTable is ARGB
                var regex = new Regex(@"^#(?<a>[0-9a-fA-F]{2})(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})?$");
                match = regex.Match(hexStringColor);
            }
            else
            {
                // CSS format is RGBA
                var regex = new Regex(@"^#(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})(?<a>[0-9a-fA-F]{2})?$");
                match = regex.Match(val);
            }

            if (match.Success)
            {
                var r = byte.Parse(match.Groups["r"].Value, System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(match.Groups["g"].Value, System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(match.Groups["b"].Value, System.Globalization.NumberStyles.HexNumber);
                var a = match.Groups["a"].Captures.Count == 1 ? byte.Parse(match.Groups["a"].Value, System.Globalization.NumberStyles.HexNumber) : 255;

                return new ColorValue(irContext, Color.FromArgb(a, r, g, b));
            }

            return CommonErrors.InvalidColorFormatError(irContext);
        }

        public static FormulaValue RGBA(IRContext irContext, NumberValue[] args)
        {
            if (args.Length != 4)
            {
                return CommonErrors.GenericInvalidArgument(irContext);
            }

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
    }
}
