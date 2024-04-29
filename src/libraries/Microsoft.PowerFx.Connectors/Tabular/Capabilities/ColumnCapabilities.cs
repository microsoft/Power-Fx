// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal sealed class ColumnCapabilities : ColumnCapabilitiesBase, IColumnsCapabilities
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ColumnProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ColumnCapabilitiesBase> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private readonly Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        [JsonInclude]
        [JsonPropertyName(Constants.XMsCapabilities)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly ColumnCapabilitiesDefinition Capabilities;

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }

        public ColumnCapabilities(ColumnCapabilitiesDefinition capability)
        {
            Contracts.AssertValueOrNull(capability);

            Capabilities = capability;
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }

        public static ColumnCapabilities ParseColumnCapabilities(OpenApiObject capabilitiesMetaData)
        {
            string[] filterFunctions = ServiceCapabilities.ParseFilterFunctions(capabilitiesMetaData);

            // Sharepoint specific capabilities
            OpenApiObject sp = capabilitiesMetaData.GetObject(CapabilityConstants.SPDelegationSupport);
            string propertyAlias = sp?.GetStr(CapabilityConstants.SPQueryName);
            bool? isChoice = sp?.GetNullableBool(CapabilityConstants.SPIsChoice);

            if (filterFunctions == null && propertyAlias == null && isChoice == null)
            {
                return null;
            }

            return new ColumnCapabilities(new ColumnCapabilitiesDefinition(filterFunctions, propertyAlias, isChoice));
        }
    }
}
