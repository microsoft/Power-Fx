// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

internal class DummyProperty : IExternalControlProperty
{
    public bool IsImmutableOnInstance { get; set; } = false;

    public bool SupportsPaging { get; set; } = false;

    public bool RequiresDefaultablePropertyReferences { get; set; } = false;

    public bool ShouldIncludeThisItemInFormula { get; set; } = false;

    public bool IsTestCaseProperty { get; set; } = false;

    public bool UseForDataQuerySelects { get; set; } = false;

    public bool IsTypeInferredFromPrimaryInput { get; set; } = false;

    public PropertyRuleCategory PropertyCategory { get; set; } = PropertyRuleCategory.Data;

    public bool IsScopeVariable { get; set; } = false;

    public string UnloadedDefault { get; set; } = string.Empty;

    public IExternalControlProperty PassThroughInput => new DummyProperty();

    public DName InvariantName { get; set; } = new DName("DummyProperty");

    public bool IsScopedProperty { get; set; } = false;

    public TexlFunction ScopeFunctionPrototype { get; set; } = null;

    public DType Type { get; set; } = DType.Unknown;

    public DType GetOpaqueType()
    {
        return Type;
    }
}
