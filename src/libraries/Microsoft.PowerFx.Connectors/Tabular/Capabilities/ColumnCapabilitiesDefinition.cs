// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors.Tabular
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

            FilterFunctions = filterFunction;
            QueryAlias = alias;
            IsChoice = isChoice;
        }
    }
}
