// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sum(arg1:n, arg2:n, ..., argN:n)
    internal sealed class SumFunction : StatisticalFunction
    {
        public override bool RequiresErrorContext => true;

        public SumFunction()
            : base("Sum", TexlStrings.AboutSum, FunctionCategories.MathAndStat)
        { }
    }

    // Sum(source:*, projection:n)
    // Corresponding DAX functions: Sum, SumX
    internal sealed class SumTableFunction : StatisticalTableFunction
    {
        public override bool RequiresErrorContext => true;

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Sum;

        public SumTableFunction()
            : base("Sum", TexlStrings.AboutSumT, FunctionCategories.Table)
        { }
    }
}
