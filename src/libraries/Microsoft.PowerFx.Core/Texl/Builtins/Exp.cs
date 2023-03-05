// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Exp(number:n)
    // Equivalent Excel function: Exp
    internal sealed class ExpFunction : MathFunction
    {
        public override bool HasPreciseErrors => true;

        public ExpFunction()
            : base("Exp", TexlStrings.AboutExp, FunctionCategories.MathAndStat)
        {
        }
    }

    // Exp(E:*[n])
    // Table overload that computes the E raised to the respective values of each item in the input table.
    internal sealed class ExpTableFunction : MathTableFunction
    {
        public override bool HasPreciseErrors => true;

        public ExpTableFunction()
            : base("Exp", TexlStrings.AboutExpT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
