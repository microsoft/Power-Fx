// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Trim(arg:s)
    internal sealed class TrimFunction : StringOneArgFunction
    {
        public TrimFunction()
            : base("Trim", TexlStrings.AboutTrim, FunctionCategories.Text)
        {
        }
    }

    // Trim(arg:*[s])
    internal sealed class TrimTFunction : StringOneArgTableFunction
    {
        public TrimTFunction()
            : base("Trim", TexlStrings.AboutTrim, FunctionCategories.Table)
        {
        }
    }

    // TrimEnds(arg:s)
    internal sealed class TrimEndsFunction : StringOneArgFunction
    {
        public TrimEndsFunction()
            : base("TrimEnds", TexlStrings.AboutTrimEnds, FunctionCategories.Text)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Trim;

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            return base.IsRowScopedServerDelegatable(callNode, binding, metadata);
        }
    }

    // Trim(arg:*[s])
    internal sealed class TrimEndsTFunction : StringOneArgTableFunction
    {
        public TrimEndsTFunction()
            : base("TrimEnds", TexlStrings.AboutTrimEnds, FunctionCategories.Table)
        {
        }
    }
}
