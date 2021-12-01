using Microsoft.PowerFx.Core.App.Controls;

namespace Microsoft.PowerFx.Core.Types
{
    internal interface IExternalControlType
    {
        IExternalControlTemplate ControlTemplate { get; }
        bool IsDataLimitedControl { get; }
        bool IsMetaField { get; }
    }
}