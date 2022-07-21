using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IUsesFeatures
    {
        bool AllowsRowScopedParamDelegationExempted(int index, Features features);
    }
}
