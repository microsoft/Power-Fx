// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sqrt(number:n)
    // Equivalent DAX function: Sqrt
    internal sealed class SqrtFunction : MathOneArgFunction
    {
        public SqrtFunction()
            : base("Sqrt", TexlStrings.AboutSqrt, FunctionCategories.MathAndStat)
        {
        }      
    }

    // Sqrt(E:*[n])
    // Table overload that computes the square root values of each item in the input table.
    internal sealed class SqrtTableFunction : MathOneArgTableFunction
    {
        public SqrtTableFunction()
            : base("Sqrt", TexlStrings.AboutSqrtT, FunctionCategories.Table)
        {
        }
    }
}
