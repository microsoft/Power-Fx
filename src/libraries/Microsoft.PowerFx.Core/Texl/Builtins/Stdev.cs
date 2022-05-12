// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // StdevP(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel function: STDEV.P
    internal sealed class StdevPFunction : StatisticalFunction
    {
        public StdevPFunction()
            : base("StdevP", TexlStrings.AboutStdevP, FunctionCategories.MathAndStat)
        {
        }
    }

    // StdevP(source:*, projection:n)
    // Corresponding DAX function: STDEV.P
    internal sealed class StdevPTableFunction : StatisticalTableFunction
    {
        public StdevPTableFunction()
            : base("StdevP", TexlStrings.AboutStdevPT, FunctionCategories.Table)
        {
        }
    }
}
