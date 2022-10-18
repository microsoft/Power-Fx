// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Lower(arg:s)
    // Upper(arg:s)
    // Corresponding DAX functions: Lower, Upper
    internal sealed class LowerUpperFunction : StringOneArgFunction
    {
        private readonly bool _isLower;

        public LowerUpperFunction(TexlFunctionConfig instanceConfig, bool isLower)
            : base(instanceConfig, isLower ? "Lower" : "Upper", isLower ? TexlStrings.AboutLower : TexlStrings.AboutUpper, FunctionCategories.Text)
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
        public LowerUpperTFunction(TexlFunctionConfig instanceConfig, bool isLower)
            : base(instanceConfig, isLower ? "Lower" : "Upper", isLower ? TexlStrings.AboutLowerT : TexlStrings.AboutUpperT, FunctionCategories.Table)
        {
        }
    }
}
