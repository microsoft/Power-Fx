// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Cot(number:n)
    // Equivalent Excel function: Cot
    internal sealed class CotFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public CotFunction()
            : base("Cot", TexlStrings.AboutCot, FunctionCategories.MathAndStat)
        {
        }
    }

    // Cot(E:*[n])
    // Table overload that computes the cotangent of each item in the input table.
    internal sealed class CotTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public CotTableFunction()
            : base("Cot", TexlStrings.AboutCotT, FunctionCategories.Table)
        {
        }
    }
}
