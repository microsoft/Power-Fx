// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Cos(number:n)
    // Equivalent Excel function: Cos
    internal sealed class CosFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public CosFunction()
            : base("Cos", TexlStrings.AboutCos, FunctionCategories.MathAndStat)
        {
        }
    }

    // Cos(E:*[n])
    // Table overload that computes the cosine of each item in the input table.
    internal sealed class CosTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public CosTableFunction()
            : base("Cos", TexlStrings.AboutCosT, FunctionCategories.Table)
        {
        }
    }
}
