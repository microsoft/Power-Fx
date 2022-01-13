// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(source:*, projection:n)
    // Max(source:*, projection:n)
    // Corresponding DAX functions: Min, MinA, MinX, Max, MaxA, MaxX
    internal sealed class MinMaxTableFunction : StatisticalTableFunction
    {
        private readonly DelegationCapability _delegationCapability;

        public override bool RequiresErrorContext => true;

        public override DelegationCapability FunctionDelegationCapability => _delegationCapability;

        public MinMaxTableFunction(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMinT : TexlStrings.AboutMaxT, FunctionCategories.Table)
        {
            _delegationCapability = isMin ? DelegationCapability.Min : DelegationCapability.Max;
        }
    }
}