// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Exp(number:n)
    // Equivalent Excel function: Exp
    internal sealed class ExpFunction : MathOneArgFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool RequiresErrorContext => true;

        public ExpFunction()
            : base("Exp", TexlStrings.AboutExp, FunctionCategories.MathAndStat)
        {
        }
    }

    // Exp(E:*[n])
    // Table overload that computes the E raised to the respective values of each item in the input table.
    internal sealed class ExpTableFunction : MathOneArgTableFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool RequiresErrorContext => true;

        public ExpTableFunction()
            : base("Exp", TexlStrings.AboutExpT, FunctionCategories.Table)
        {
        }
    }
}
