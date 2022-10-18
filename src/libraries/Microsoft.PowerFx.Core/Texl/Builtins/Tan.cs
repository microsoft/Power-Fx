// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Tan(number:n)
    // Equivalent Excel function: Tan
    internal sealed class TanFunction : MathOneArgFunction
    {
        public TanFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Tan", TexlStrings.AboutTan, FunctionCategories.MathAndStat)
        {
        }
    }

    // Tan(E:*[n])
    // Table overload that computes the tangent of each item in the input table.
    internal sealed class TanTableFunction : MathOneArgTableFunction
    {
        public TanTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Tan", TexlStrings.AboutTanT, FunctionCategories.Table)
        {
        }
    }
}
