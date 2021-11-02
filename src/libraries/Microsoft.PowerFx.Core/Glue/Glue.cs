// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Core.Glue
{
    // $$$ Everything in this file should get removed. 
    internal class Glue2DocumentBinderGlue : IBinderGlue
    {
        public bool CanControlBeUsedInComponentProperty(TexlBinding binding, IExternalControl control)
        {
            throw new NotImplementedException();
        }

        public IExternalControl GetVariableScopedControlFromTexlBinding(TexlBinding txb)
        {
            throw new NotImplementedException();
        }

        public bool IsComponentDataSource(object lookupInfoData)
        {
            throw new NotImplementedException();
        }

        public bool IsComponentScopedPropertyFunction(TexlFunction infoFunction)
        {
            return false; // $$$
        }

        public bool IsContextProperty(IExternalControlProperty externalControlProperty)
        {
            throw new NotImplementedException();
        }

        public bool IsDataComponentDefinition(object lookupInfoData)
        {
            throw new NotImplementedException();
        }

        public bool IsDataComponentInstance(object lookupInfoData)
        {
            throw new NotImplementedException();
        }

        public bool IsDynamicDataSourceInfo(object lookupInfoData)
        {
            return false; // TODO: ?
        }

        public bool IsPrimaryCommandComponentProperty(IExternalControlProperty externalControlProperty)
        {
            throw new NotImplementedException();
        }

        public bool TryGetCdsDataSourceByBind(object lhsInfoData, out IExternalControl o)
        {
            throw new NotImplementedException();
        }
    }
}
