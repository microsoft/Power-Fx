// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abs(number:n)
    // Equivalent DAX function: Abs
    internal sealed class AbsFunction : MathOneArgFunction
    {
        public AbsFunction()
            : base("Abs", TexlStrings.AboutAbs, FunctionCategories.MathAndStat, DecimalOverload.Number)
        {
        }
    }

    // Abs(E:*[n])
    // Table overload that computes the absolute values of each item in the input table.
    internal sealed class AbsTableFunction : MathOneArgTableFunction
    {
        public AbsTableFunction()
            : base("Abs", TexlStrings.AboutAbsT, FunctionCategories.Table, DecimalOverload.Number)
        {
        }
    }

    // Abs(number:w)
    // Equivalent DAX function: Abs
    internal sealed class AbsWFunction : MathOneArgFunction
    {
        public AbsWFunction()
            : base("Abs", TexlStrings.AboutAbs, FunctionCategories.MathAndStat, DecimalOverload.Decimal)
        {
        }
    }

    // Abs(E:*[n])
    // Table overload that computes the absolute values of each item in the input table.
    internal sealed class AbsWTableFunction : MathOneArgTableFunction
    {
        public AbsWTableFunction()
            : base("Abs", TexlStrings.AboutAbsT, FunctionCategories.Table, DecimalOverload.Decimal)
        {
        }
    }
}
