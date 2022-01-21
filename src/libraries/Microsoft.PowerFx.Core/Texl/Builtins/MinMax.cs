// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(arg1:n, arg2:n, ..., argN:n)
    // Max(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel functions: Min, Max
    internal sealed class MinMaxFunction : StatisticalFunction
    {
        public override bool SupportsParamCoercion => true;

        public MinMaxFunction(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMin : TexlStrings.AboutMax, FunctionCategories.MathAndStat)
        {
        }
    }
}
