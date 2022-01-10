// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Lower(arg:s)
    // Upper(arg:s)
    // Corresponding DAX functions: Lower, Upper
    internal sealed class LowerUpperFunction : StringOneArgFunction
    {
        private readonly bool _isLower;
        public LowerUpperFunction(bool isLower)
            : base(isLower ? "Lower" : "Upper", isLower ? TexlStrings.AboutLower : TexlStrings.AboutUpper, FunctionCategories.Text)
        {
            _isLower = isLower;
        }

        public override DelegationCapability FunctionDelegationCapability => _isLower ? DelegationCapability.Lower : DelegationCapability.Upper;

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            return false;
        }
    }

    // Lower(arg:*[s])
    // Upper(arg:*[s])
    internal sealed class LowerUpperTFunction : StringOneArgTableFunction
    {
        public LowerUpperTFunction(bool isLower)
            : base(isLower ? "Lower" : "Upper", isLower ? TexlStrings.AboutLowerT : TexlStrings.AboutUpperT, FunctionCategories.Table)
        { }
    }
}
