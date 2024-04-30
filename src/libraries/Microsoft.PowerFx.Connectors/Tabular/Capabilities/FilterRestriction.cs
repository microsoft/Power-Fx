// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal sealed class FilterRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterRequiredProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly List<string> RequiredProperties;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.NonFilterableProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly List<string> NonFilterableProperties;

        public FilterRestriction(List<string> requiredProperties, List<string> nonFilterableProperties)
        {
            Contracts.AssertValueOrNull(requiredProperties);
            Contracts.AssertValueOrNull(nonFilterableProperties);

            RequiredProperties = requiredProperties;
            NonFilterableProperties = nonFilterableProperties;
        }
    }
}
