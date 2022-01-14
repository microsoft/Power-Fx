// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // VarP(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel function: VARP
    internal sealed class VarPFunction : StatisticalFunction
    {
        public override bool RequiresErrorContext => true;

        public VarPFunction()
            : base("VarP", TexlStrings.AboutVarP, FunctionCategories.MathAndStat)
        {
        }
    }

    // VarP(source:*, projection:n)
    // Corresponding DAX function: VAR.P
    internal sealed class VarPTableFunction : StatisticalTableFunction
    {
        public override bool RequiresErrorContext => true;

        public VarPTableFunction()
            : base("VarP", TexlStrings.AboutVarPT, FunctionCategories.Table)
        {
        }
    }
}