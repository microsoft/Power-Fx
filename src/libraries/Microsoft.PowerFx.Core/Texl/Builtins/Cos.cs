// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Cos(number:n)
    // Equivalent Excel function: Cos
    internal sealed class CosFunction : MathFunction
    {
        public CosFunction()
            : base("Cos", TexlStrings.AboutCos, FunctionCategories.MathAndStat)
        {
        }
    }

    // Cos(E:*[n])
    // Table overload that computes the cosine of each item in the input table.
    internal sealed class CosTableFunction : MathTableFunction
    {
        public CosTableFunction()
            : base("Cos", TexlStrings.AboutCosT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
