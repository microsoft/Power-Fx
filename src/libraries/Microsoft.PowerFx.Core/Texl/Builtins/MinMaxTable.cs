// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(source:*, projection:n)
    // Max(source:*, projection:n)
    // Corresponding DAX functions: Min, MinA, MinX, Max, MaxA, MaxX
    internal sealed class MinTableFunction : StatisticalTableFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Min;

        public MinTableFunction()
            : base("Min", TexlStrings.AboutMinT, FunctionCategories.Table, nativeDecimal: true, nativeDateTime: true)
        {
        }
    }

    internal sealed class MaxTableFunction : StatisticalTableFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Max;

        public MaxTableFunction()
            : base("Max", TexlStrings.AboutMaxT, FunctionCategories.Table, nativeDecimal: true, nativeDateTime: true)
        {
        }
    }
}
