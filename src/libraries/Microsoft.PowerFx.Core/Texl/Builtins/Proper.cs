// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Proper(arg:s)
    internal sealed class ProperFunction : StringOneArgFunction
    {
        public override bool HasPreciseErrors => true;

        public ProperFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Proper", TexlStrings.AboutProper, FunctionCategories.Text)
        {
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            return false;
        }
    }

    // Proper(arg:*[s])
    internal sealed class ProperTFunction : StringOneArgTableFunction
    {
        public override bool HasPreciseErrors => true;

        public ProperTFunction(TexlFunctionConfig instanceConfig)
            : base(instanceConfig, "Proper", TexlStrings.AboutProperT, FunctionCategories.Table)
        {
        }
    }
}
