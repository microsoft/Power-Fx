// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Average(arg1:n|w, arg2:n|w, ..., argN:n|w)
    // Corresponding Excel function: Average
    internal sealed class AverageFunction : StatisticalFunction
    {
        public AverageFunction()
            : base("Average", TexlStrings.AboutAverage, FunctionCategories.MathAndStat, nativeDecimal: true)
        {
        }
    }

    // Average(source:*, projection:n|w)
    // Corresponding DAX function: Average
    internal sealed class AverageTableFunction : StatisticalTableFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Average;

        public AverageTableFunction()
            : base("Average", TexlStrings.AboutAverageT, FunctionCategories.Table, nativeDecimal: true)
        {
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
