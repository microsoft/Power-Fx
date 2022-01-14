// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Int(number:n)
    // Truncate by rounding toward negative infinity.
    internal sealed class IntFunction : MathOneArgFunction
    {
        public IntFunction()
            : base("Int", TexlStrings.AboutInt, FunctionCategories.MathAndStat)
        {
        }
    }

    // Int(E:*[n])
    // Table overload that applies Int to each item in the input table.
    internal sealed class IntTableFunction : MathOneArgTableFunction
    {
        public IntTableFunction()
            : base("Int", TexlStrings.AboutIntT, FunctionCategories.Table)
        {
        }
    }
}
