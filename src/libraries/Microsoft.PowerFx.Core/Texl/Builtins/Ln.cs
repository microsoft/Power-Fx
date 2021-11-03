// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Ln(number:n)
    // Equivalent Excel function: Ln
    internal sealed class LnFunction : MathOneArgFunction
    {
        public override bool HasPreciseErrors => true;
        public LnFunction()
            : base("Ln", TexlStrings.AboutLn, FunctionCategories.MathAndStat)
        { }
    }

    // Ln(E:*[n])
    // Table overload that computes the natural logarithm values of each item in the input table.
    internal sealed class LnTableFunction : MathOneArgTableFunction
    {
        public override bool HasPreciseErrors => true;
        public LnTableFunction()
            : base("Ln", TexlStrings.AboutLnT, FunctionCategories.Table)
        { }
    }
}
