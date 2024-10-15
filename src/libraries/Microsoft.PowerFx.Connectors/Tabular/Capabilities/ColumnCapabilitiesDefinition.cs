// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    internal sealed class ColumnCapabilitiesDefinition
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterFunctions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string> FilterFunctions { get; }

        // used in PowerApps-Client/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.PropertyQueryAlias)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        internal string QueryAlias { get; init; }

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SPIsChoice)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        internal bool? IsChoice { get; init; }
        
        public ColumnCapabilitiesDefinition(IEnumerable<string> filterFunction)
        {                   
            // ex: lt, le, eq, ne, gt, ge, and, or, not, contains, startswith, endswith, countdistinct, day, month, year, time
            FilterFunctions = filterFunction;           
        }
    }
}
