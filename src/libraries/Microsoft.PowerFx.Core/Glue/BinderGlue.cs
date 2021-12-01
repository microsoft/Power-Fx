using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Core.Glue
{
    /// <summary>
    /// A class that exists solely to provide an inactive stub for the
    /// Language-side binder. This resolves static dependencies on App related
    /// code that may exist, which we don't want to move over to Language even
    /// temporarily.
    /// </summary>
    internal interface IBinderGlue
    {
        bool IsDataComponentDefinition(object lookupInfoData);
        bool IsComponentDataSource(object lookupInfoData);
        bool IsDataComponentInstance(object lookupInfoData);
        bool TryGetCdsDataSourceByBind(object lhsInfoData, out IExternalControl o);
        bool IsDynamicDataSourceInfo(object lookupInfoData);
        bool CanControlBeUsedInComponentProperty(TexlBinding binding, IExternalControl control);
        IExternalControl GetVariableScopedControlFromTexlBinding(TexlBinding txb);
        bool IsComponentScopedPropertyFunction(TexlFunction infoFunction);
        bool IsPrimaryCommandComponentProperty(IExternalControlProperty externalControlProperty);
        bool IsContextProperty(IExternalControlProperty externalControlProperty);
    }
}