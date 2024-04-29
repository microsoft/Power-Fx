// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal sealed class SelectionRestriction
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.Selectable)]
        public readonly bool IsSelectable;

        public SelectionRestriction(bool isSelectable)
        {
            IsSelectable = isSelectable;
        }
    }
}
