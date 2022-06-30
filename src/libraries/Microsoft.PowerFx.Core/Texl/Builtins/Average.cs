// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Average(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel function: Average
    internal sealed class AverageFunction : StatisticalFunction
    {
        public AverageFunction()
            : base("Average", TexlStrings.AboutAverage, FunctionCategories.MathAndStat)
        {
        }
    }

    // Average(source:*, projection:n)
    // Corresponding DAX function: Average
    internal sealed class AverageTableFunction : StatisticalTableFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Average;

        public AverageTableFunction()
            : base("Average", TexlStrings.AboutAverageT, FunctionCategories.Table)
        {
        }
    }
}
