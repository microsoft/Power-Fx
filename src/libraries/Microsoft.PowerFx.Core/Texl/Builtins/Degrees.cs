// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Degrees(number:n)
    // Equivalent Excel function: Degrees
    internal sealed class DegreesFunction : MathFunction
    {
        public DegreesFunction()
            : base("Degrees", TexlStrings.AboutDegrees, FunctionCategories.MathAndStat)
        {
        }
    }

    // Degrees(E:*[n])
    // Table overload that computes the degrees values of each item in the input table.
    internal sealed class DegreesTableFunction : MathOneArgTableFunction
    {
        public DegreesTableFunction()
            : base("Degrees", TexlStrings.AboutDegreesT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
