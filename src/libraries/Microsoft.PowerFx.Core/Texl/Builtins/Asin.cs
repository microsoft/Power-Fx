// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Asin(number:n)
    // Equivalent Excel function: Asin
    internal sealed class AsinFunction : MathOneArgFunction
    {
        public AsinFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Asin", TexlStrings.AboutAsin, FunctionCategories.MathAndStat)
        {
        }
    }

    // Asin(E:*[n])
    // Table overload that computes the arc sine values of each item in the input table.
    internal sealed class AsinTableFunction : MathOneArgTableFunction
    {
        public AsinTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Asin", TexlStrings.AboutAsinT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
