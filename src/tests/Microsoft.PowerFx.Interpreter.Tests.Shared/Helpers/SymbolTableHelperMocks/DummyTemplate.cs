// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.Components;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Utils;

internal class DummyTemplate : IExternalControlTemplate
{
    public readonly Dictionary<string, IExternalControlProperty> InputProps = new ();

    public readonly Dictionary<string, IExternalControlProperty> OutputProps = new ();

    public ComponentType ComponentType { get; set; } = ComponentType.FunctionComponent;

    public bool IncludesThisItemInSpecificProperty { get; set; } = false;

    public bool ReplicatesNestedControls { get; set; } = false;

    public IEnumerable<DName> NestedAwareTableOutputs { get; set; } = Enumerable.Empty<DName>();

    public bool IsComponent { get; set; } = false;

    public bool HasExpandoProperties { get; set; } = false;

    public IEnumerable<IExternalControlProperty> ExpandoProperties { get; set; } = Enumerable.Empty<IExternalControlProperty>();

    public bool HasPropsInferringTypeFromPrimaryInProperty { get; set; } = false;

    public IEnumerable<IExternalControlProperty> PropsInferringTypeFromPrimaryInProperty => Enumerable.Empty<IExternalControlProperty>();  

    public string ThisItemInputInvariantName { get; set; } = string.Empty;

    public IExternalControlProperty PrimaryOutputProperty { get; set; } = new DummyProperty();

    public bool IsMetaLoc { get; set; } = false;

    public bool IsCommandComponent { get; set; } = false;

    public bool HasOutput(DName rightName)
    {
        return OutputProps.ContainsKey(rightName.Value);
    }

    public bool HasProperty(string currentPropertyValue, PropertyRuleCategory category)
    {
        return TryGetProperty(currentPropertyValue, out _);
    }

    public bool TryGetInputProperty(string resolverCurrentProperty, out IExternalControlProperty currentProperty)
    {
        return InputProps.TryGetValue(resolverCurrentProperty, out currentProperty);
    }

    public bool TryGetOutputProperty(string name, out IExternalControlProperty externalControlProperty)
    {
        return OutputProps.TryGetValue(name, out externalControlProperty);
    }

    public bool TryGetProperty(string name, out IExternalControlProperty controlProperty)
    {
        return TryGetInputProperty(name, out controlProperty) || TryGetOutputProperty(name, out controlProperty);
    }
}
