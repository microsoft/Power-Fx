// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalControlProperty
    {
        bool IsImmutableOnInstance { get; }

        bool SupportsPaging { get; }

        bool RequiresDefaultablePropertyReferences { get; }

        bool ShouldIncludeThisItemInFormula { get; }

        DName Name { get; }

        bool IsTestCaseProperty { get; }

        bool UseForDataQuerySelects { get; }

        PropertyRuleCategory PropertyCategory { get; }

        bool IsScopeVariable { get; }

        string UnloadedDefault { get; }

        IExternalControlProperty PassThroughInput { get; }

        DName InvariantName { get; }

        bool IsScopedProperty { get; }

        TexlFunction ScopeFunctionPrototype { get; }

        DType Type { get; }

        bool IsTable { get; }

        bool IsEnum { get; }

        bool Hidden { get; }

        DType GetOpaqueType();
    }
}