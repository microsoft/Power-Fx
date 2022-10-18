// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // VarP(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel function: VARP
    internal sealed class VarPFunction : StatisticalFunction
    {
        public VarPFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "VarP", TexlStrings.AboutVarP, FunctionCategories.MathAndStat)
        {
        }
    }

    // VarP(source:*, projection:n)
    // Corresponding DAX function: VAR.P
    internal sealed class VarPTableFunction : StatisticalTableFunction
    {
        public VarPTableFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "VarP", TexlStrings.AboutVarPT, FunctionCategories.Table)
        {
        }
    }
}
