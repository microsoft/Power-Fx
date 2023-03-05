// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abs(number:n|w)
    // Equivalent DAX function: Abs
    internal sealed class AbsFunction : MathFunction
    {
        public AbsFunction()
            : base("Abs", TexlStrings.AboutAbs, FunctionCategories.MathAndStat, nativeDecimal: true)
        {
        }
    }

    // Abs(E:*[n|w])
    // Table overload that computes the absolute values of each item in the input table.
    internal sealed class AbsTableFunction : MathTableFunction
    {
        public AbsTableFunction()
            : base("Abs", TexlStrings.AboutAbsT, FunctionCategories.Table, nativeDecimal: true)
        {
        }
    }
}
