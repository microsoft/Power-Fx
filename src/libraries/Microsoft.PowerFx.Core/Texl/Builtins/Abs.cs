// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abs(number:n)
    // Equivalent DAX function: Abs
    internal sealed class AbsFunction : MathOneArgFunction
    {
        public AbsFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Abs", TexlStrings.AboutAbs, FunctionCategories.MathAndStat)
        {
        }
    }

    // Abs(E:*[n])
    // Table overload that computes the absolute values of each item in the input table.
    internal sealed class AbsTableFunction : MathOneArgTableFunction
    {
        public AbsTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Abs", TexlStrings.AboutAbsT, FunctionCategories.Table)
        {
        }
    }
}
