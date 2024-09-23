// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

internal class DummyExternalControl : IExternalControl
{
    public IExternalControlTemplate Template => null;

    public bool IsComponentControl { get; set; } = false;

    public IExternalControl TopParentOrSelf => this;

    public string DisplayName { get; set; } = "DummyExternalControl";

    public bool IsReplicable { get; set; } = false;

    public bool IsAppInfoControl { get; set; } = false;

    public DType ThisItemType { get; set; } = DType.Unknown;

    public bool IsAppGlobalControl => IsAppInfoControl;

    public bool IsCommandComponentInstance { get; set; } = false;

    public DName EntityName => new DName(DisplayName);

    public DType Type { get; set; } = DType.Unknown;

    public virtual IExternalRule GetRule(string propertyInvariantName)
    {
        return null;
    }

    public virtual bool IsDescendentOf(IExternalControl controlInfo)
    {
        return false;
    }

    public virtual bool TryGetRule(string dName, out IExternalRule rule)
    {
        rule = GetRule(dName);
        return rule != null;
    }
}
