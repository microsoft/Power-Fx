// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Cot(number:n)
    // Equivalent Excel function: Cot
    internal sealed class CotFunction : MathOneArgFunction
    {
        public CotFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Cot", TexlStrings.AboutCot, FunctionCategories.MathAndStat)
        {
        }
    }

    // Cot(E:*[n])
    // Table overload that computes the cotangent of each item in the input table.
    internal sealed class CotTableFunction : MathOneArgTableFunction
    {
        public CotTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Cot", TexlStrings.AboutCotT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
