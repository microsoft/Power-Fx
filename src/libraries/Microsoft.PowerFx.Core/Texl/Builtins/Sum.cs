// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sum(arg1:n, arg2:n, ..., argN:n)
    internal sealed class SumFunction : StatisticalFunction
    {
        public SumFunction()
            : base("Sum", TexlStrings.AboutSum, FunctionCategories.MathAndStat, nativeDecimal: true)
        {
        }
    }

    // Sum(source:*, projection:n)
    // Corresponding DAX functions: Sum, SumX
    internal sealed class SumTableFunction : StatisticalTableFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Sum;

        public SumTableFunction()
            : base("Sum", TexlStrings.AboutSumT, FunctionCategories.Table, nativeDecimal: true)
        {
        }
    }
}
