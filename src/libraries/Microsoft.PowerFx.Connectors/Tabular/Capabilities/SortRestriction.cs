﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal sealed class SortRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.AscendingOnlyProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly List<string> AscendingOnlyProperties;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.UnsortableProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly List<string> UnsortableProperties;

        public SortRestriction(List<string> unsortableProperties, List<string> ascendingOnlyProperties)
        {
            Contracts.AssertValueOrNull(unsortableProperties);
            Contracts.AssertValueOrNull(ascendingOnlyProperties);

            AscendingOnlyProperties = ascendingOnlyProperties;
            UnsortableProperties = unsortableProperties;
        }
    }
}
