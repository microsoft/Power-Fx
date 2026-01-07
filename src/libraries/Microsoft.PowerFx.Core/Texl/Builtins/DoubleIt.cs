// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // DoubleIt(number:n)
    // Returns 2 * number
    internal sealed class DoubleItFunction : MathOneArgFunction
    {
        public DoubleItFunction()
            : base("DoubleIt", TexlStrings.AboutDoubleIt, FunctionCategories.MathAndStat, nativeDecimal: true)
        {
        }
    }
}
