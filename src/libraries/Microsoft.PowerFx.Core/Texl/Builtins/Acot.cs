// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Acot(number:n)
    // Equivalent Excel function: Acot
    internal sealed class AcotFunction : MathFunction
    {
        public AcotFunction()
            : base("Acot", TexlStrings.AboutAcot, FunctionCategories.MathAndStat)
        {
        }
    }

    // Acot(E:*[n])
    // Table overload that computes the arc cotangent of each item in the input table.
    internal sealed class AcotTableFunction : MathTableFunction
    {
        public AcotTableFunction()
            : base("Acot", TexlStrings.AboutAcotT, FunctionCategories.Table)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
