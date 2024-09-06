// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

internal class ControlVirtualType : DType, IExternalControlType
{
    public ControlVirtualType(TypeTree typeTree = default)
        : base(typeTree)
    {
        foreach (var (key, value) in typeTree)
        {
            Template.OutputProps.Add(key, new DummyProperty() { InvariantName = new DName(key), Type = value });
        }
    }
    
    public readonly DummyTemplate Template = new ();

    public IExternalControlTemplate ControlTemplate => Template;

    public bool IsMetaField => false;
}
