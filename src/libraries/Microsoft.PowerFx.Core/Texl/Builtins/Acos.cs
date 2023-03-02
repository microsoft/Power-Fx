// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Acos(number:n)
    // Equivalent Excel function: acos
    internal sealed class AcosFunction : MathFunction
    {
        public AcosFunction()
            : base("Acos", TexlStrings.AboutAcos, FunctionCategories.MathAndStat)
        {
        }
    }

    // Acos(E:*[n])
    // Table overload that computes the arc cosine of each item in the input table.
    internal sealed class AcosTableFunction : MathOneArgTableFunction
    {
        public AcosTableFunction()
            : base("Acos", TexlStrings.AboutAcosT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
