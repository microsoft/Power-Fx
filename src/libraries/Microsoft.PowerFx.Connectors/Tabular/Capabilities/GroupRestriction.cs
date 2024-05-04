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
    internal sealed class GroupRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.UngroupableProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly List<string> UngroupableProperties;

        public GroupRestriction(List<string> ungroupableProperties)
        {
            Contracts.AssertValueOrNull(ungroupableProperties);

            UngroupableProperties = ungroupableProperties;
        }
    }
}
