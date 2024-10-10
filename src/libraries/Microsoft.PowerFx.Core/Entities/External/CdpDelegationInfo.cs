// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Entities
{
    internal class CdpDelegationInfo : TableDelegationInfo
    {
        public override ColumnCapabilitiesDefinition GetColumnCapability(string fieldName)
        {
            // We should never reach that point in CDP case
            throw new System.NotImplementedException();
        }
    }
}
