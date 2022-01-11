// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Asin(number:n)
    // Equivalent Excel function: Asin
    internal sealed class AsinFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public AsinFunction()
            : base("Asin", TexlStrings.AboutAsin, FunctionCategories.MathAndStat)
        {
        }
    }

    // Asin(E:*[n])
    // Table overload that computes the arc sine values of each item in the input table.
    internal sealed class AsinTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public AsinTableFunction()
            : base("Asin", TexlStrings.AboutAsinT, FunctionCategories.Table)
        {
        }
    }
}