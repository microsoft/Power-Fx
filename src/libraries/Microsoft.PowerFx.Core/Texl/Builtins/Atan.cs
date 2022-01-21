// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Atan(number:n)
    // Equivalent Excel function: Atan
    internal sealed class AtanFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public AtanFunction()
            : base("Atan", TexlStrings.AboutAtan, FunctionCategories.MathAndStat)
        {
        }
    }

    // Atan(E:*[n])
    // Table overload that computes the arc tangent of each item in the input table.
    internal sealed class AtanTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public AtanTableFunction()
            : base("Atan", TexlStrings.AboutAtanT, FunctionCategories.Table)
        {
        }
    }
}
