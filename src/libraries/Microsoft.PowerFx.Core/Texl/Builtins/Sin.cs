// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sin(number:n)
    // Equivalent Excel function: Sin
    internal sealed class SinFunction : MathOneArgFunction
    {
        public SinFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Sin", TexlStrings.AboutSin, FunctionCategories.MathAndStat)
        {
        }
    }

    // Sin(E:*[n])
    // Table overload that computes the sine values of each item in the input table.
    internal sealed class SinTableFunction : MathOneArgTableFunction
    {
        public SinTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Sin", TexlStrings.AboutSinT, FunctionCategories.Table)
        {
        }
    }
}
