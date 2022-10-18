// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Atan(number:n)
    // Equivalent Excel function: Atan
    internal sealed class AtanFunction : MathOneArgFunction
    {
        public AtanFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Atan", TexlStrings.AboutAtan, FunctionCategories.MathAndStat)
        {
        }
    }

    // Atan(E:*[n])
    // Table overload that computes the arc tangent of each item in the input table.
    internal sealed class AtanTableFunction : MathOneArgTableFunction
    {
        public AtanTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Atan", TexlStrings.AboutAtanT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
