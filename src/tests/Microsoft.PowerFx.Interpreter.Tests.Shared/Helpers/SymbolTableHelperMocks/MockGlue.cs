// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;

internal class MockGlue : IBinderGlue
{
    public bool IsComponentDataSource(object lookupInfoData)
    {
        return false;
    }

    public bool IsDataComponentDefinition(object lookupInfoData)
    {
        return false;
    }

    public bool IsDataComponentInstance(object lookupInfoData)
    {
        return false;
    }

    public bool IsDynamicDataSourceInfo(object lookupInfoData)
    {
        return false;
    }

    bool IBinderGlue.CanControlBeUsedInComponentProperty(TexlBinding binding, IExternalControl control)
    {
        return true;
    }

    IExternalControl IBinderGlue.GetVariableScopedControlFromTexlBinding(TexlBinding txb)
    {
        return new DummyExternalControl();
    }

    bool IBinderGlue.IsComponentScopedPropertyFunction(TexlFunction infoFunction)
    {
        return false;
    }

    bool IBinderGlue.IsContextProperty(IExternalControlProperty externalControlProperty)
    {
        return false;
    }

    bool IBinderGlue.IsPrimaryCommandComponentProperty(IExternalControlProperty externalControlProperty)
    {
        return false;
    }

    bool IBinderGlue.TryGetCdsDataSourceByBind(object lhsInfoData, out IExternalControl o)
    {
        o = null;
        return false;
    }
}
