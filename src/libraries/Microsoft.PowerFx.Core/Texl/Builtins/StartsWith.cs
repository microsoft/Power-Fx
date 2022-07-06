// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // StartsWith(text:s, start:s):b
    // Checks if the text starts with the start string.
    internal sealed class StartsWithFunction : StringTwoArgFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.StartsWith;

        public StartsWithFunction()
            : base("StartsWith", TexlStrings.AboutStartsWith)
        {
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            return IsRowScopedServerDelegatableHelper(callNode, binding, metadata);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StartsWithArg1, TexlStrings.StartsWithArg2 };
        }

        // TASK: 856362
        // Add overload for single-column table as the input.
    }
}
