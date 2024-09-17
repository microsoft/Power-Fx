// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    public sealed class FilterRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterRequiredProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly IList<string> RequiredProperties;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.NonFilterableProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly IList<string> NonFilterableProperties;

        public FilterRestriction(IList<string> requiredProperties, IList<string> nonFilterableProperties)
        {
            Contracts.AssertValueOrNull(requiredProperties);
            Contracts.AssertValueOrNull(nonFilterableProperties);

            // List of required properties
            RequiredProperties = requiredProperties;

            // List of non filterable properties
            NonFilterableProperties = nonFilterableProperties;
        }
    }
}
