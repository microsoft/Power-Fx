// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sqrt(number:n)
    // Equivalent DAX function: Sqrt
    internal sealed class SqrtFunction : MathOneArgFunction
    {
        public SqrtFunction()
            : base("Sqrt", TexlStrings.AboutSqrt, FunctionCategories.MathAndStat)
        {
        }
    }

    // Sqrt(E:*[n])
    // Table overload that computes the square root values of each item in the input table.
    internal sealed class SqrtTableFunction : MathOneArgTableFunction
    {
        public SqrtTableFunction()
            : base("Sqrt", TexlStrings.AboutSqrtT, FunctionCategories.Table)
        {
        }
    }
}
