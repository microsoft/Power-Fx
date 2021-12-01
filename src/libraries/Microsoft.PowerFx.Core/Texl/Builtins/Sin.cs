// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sin(number:n)
    // Equivalent Excel function: Sin
    internal sealed class SinFunction : MathOneArgFunction
    {
        public override bool RequiresErrorContext => true;

        public SinFunction()
            : base("Sin", TexlStrings.AboutSin, FunctionCategories.MathAndStat)
        { }
    }

    // Sin(E:*[n])
    // Table overload that computes the sine values of each item in the input table.
    internal sealed class SinTableFunction : MathOneArgTableFunction
    {
        public override bool RequiresErrorContext => true;

        public SinTableFunction()
            : base("Sin", TexlStrings.AboutSinT, FunctionCategories.Table)
        { }
    }
}