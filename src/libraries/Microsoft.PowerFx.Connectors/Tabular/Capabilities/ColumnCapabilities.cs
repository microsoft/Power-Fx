// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    public sealed class ColumnCapabilities : ColumnCapabilitiesBase, IColumnsCapabilities
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ColumnProperties)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ColumnCapabilitiesBase> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        [JsonInclude]
        [JsonPropertyName(Constants.XMsCapabilities)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ColumnCapabilitiesDefinition Capabilities;

        public static readonly IEnumerable<string> DefaultFilterFunctionSupport = new string[] { "eq", "ne", "gt", "ge", "lt", "le", "and", "or", "cdsin", "contains", "startswith", "endswith", "not", "null", "sum", "average", "min", "max", "count", "countdistinct", "top", "astype", "arraylookup" };

        public static ColumnCapabilities DefaultCdsColumnCapabilities => new ColumnCapabilities()
        {
            Capabilities = new ColumnCapabilitiesDefinition(DefaultFilterFunctionSupport, null, null),
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>()
        };

        private ColumnCapabilities()
        {
        }

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

        public static ColumnCapabilities ParseColumnCapabilities(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            string[] filterFunctions = ServiceCapabilities.ParseFilterFunctions(capabilitiesMetaData);

            // Sharepoint specific capabilities
            IDictionary<string, IOpenApiAny> sp = capabilitiesMetaData.GetObject(CapabilityConstants.SPDelegationSupport);
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
