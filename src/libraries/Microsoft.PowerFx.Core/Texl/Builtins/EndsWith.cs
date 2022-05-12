// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // EndsWith(text:s, end:s):b
    // Checks if the text ends with the end string.
    internal sealed class EndsWithFunction : StringTwoArgFunction
    {
        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.EndsWith;

        public EndsWithFunction()
            : base("EndsWith", TexlStrings.AboutEndsWith)
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
            yield return new[] { TexlStrings.EndsWithArg1, TexlStrings.EndsWithArg2 };
        }

        // TASK: 856362
        // Add overload for single-column table as the input for both endsWith and startsWith.
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
