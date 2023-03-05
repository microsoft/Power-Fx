// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Radians(number:n)
    // Equivalent Excel function: Radians
    internal sealed class RadiansFunction : MathFunction
    {
        public RadiansFunction()
            : base("Radians", TexlStrings.AboutRadians, FunctionCategories.MathAndStat)
        {
        }
    }

    // Radians(E:*[n])
    // Table overload that computes the radians values of each item in the input table.
    internal sealed class RadiansTableFunction : MathTableFunction
    {
        public RadiansTableFunction()
            : base("Radians", TexlStrings.AboutRadiansT, FunctionCategories.Table)
        {
        }
    }
}
