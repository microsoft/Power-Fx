// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Tan(number:n)
    // Equivalent Excel function: Tan
    internal sealed class TanFunction : MathFunction
    {
        public TanFunction()
            : base("Tan", TexlStrings.AboutTan, FunctionCategories.MathAndStat)
        {
        }
    }

    // Tan(E:*[n])
    // Table overload that computes the tangent of each item in the input table.
    internal sealed class TanTableFunction : MathTableFunction
    {
        public TanTableFunction()
            : base("Tan", TexlStrings.AboutTanT, FunctionCategories.Table)
        {
        }
    }
}
