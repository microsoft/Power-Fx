// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Entities
{
    // Used by ServiceCapabilities.ToDelegationInfo for managing CDP x-ms-capabilities
    internal class CdpDelegationInfo : TableDelegationInfo
    {
        public override ColumnCapabilitiesDefinition GetColumnCapability(string fieldName)
        {
            if (ColumnsCapabilities.TryGetValue(fieldName, out ColumnCapabilitiesBase columnCapabilitiesBase) && columnCapabilitiesBase != null)
            {               
                return columnCapabilitiesBase switch
                {
                    ColumnCapabilities columnCapabilities => columnCapabilities.Definition,
                    _ => throw new NotImplementedException($"{columnCapabilitiesBase.GetType().Name} not supported yet")
                };
            }

            return null;
        }
    }
}
