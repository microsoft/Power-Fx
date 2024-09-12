// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        public readonly string[] FilterFunctions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.PropertyQueryAlias)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly string QueryAlias;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SPIsChoice)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public readonly bool? IsChoice;

        public ColumnCapabilitiesDefinition(string[] filterFunction, string alias, bool? isChoice)
        {
            Contracts.AssertValueOrNull(filterFunction);
            Contracts.AssertValueOrNull(alias);

            // ex: lt, le, eq, ne, gt, ge, and, or, not, contains, startswith, endswith, countdistinct, day, month, year, time
            FilterFunctions = filterFunction;

            // used to rename column names
            // used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts&_a=contents&version=GBmaster
            QueryAlias = alias;

            // sharepoint delegation specific
            IsChoice = isChoice;
        }
    }
}
