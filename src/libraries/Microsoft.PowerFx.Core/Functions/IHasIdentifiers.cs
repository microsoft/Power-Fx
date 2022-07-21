using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions
{
    internal interface IHasIdentifiers
    {
        bool IsIdentifierParam(int index);
    }
}
