// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    public sealed class SelectionRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.Selectable)]
        public readonly bool IsSelectable;

        public SelectionRestriction(bool isSelectable)
        {
            // Indicates whether this table has selectable columns
            // Used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/Cloud/DocumentServer.Core/Document/Document/InfoTypes/CdsDataSourceInfo.cs&_a=contents&version=GBmaster
            IsSelectable = isSelectable;
        }
    }
}
