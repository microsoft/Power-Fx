// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Asin(number:n)
    // Equivalent Excel function: Asin
    internal sealed class AsinFunction : MathOneArgFunction
    {
        public AsinFunction()
            : base("Asin", TexlStrings.AboutAsin, FunctionCategories.MathAndStat)
        {
        }
    }

    // Asin(E:*[n])
    // Table overload that computes the arc sine values of each item in the input table.
    internal sealed class AsinTableFunction : MathOneArgTableFunction
    {
        public AsinTableFunction()
            : base("Asin", TexlStrings.AboutAsinT, FunctionCategories.Table)
        {
        }
    }
}
