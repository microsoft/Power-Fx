// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    internal sealed class ColumnCapabilities : ColumnCapabilitiesBase, IColumnsCapabilities, IEquatable<ColumnCapabilities>
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
        
        public static ColumnCapabilities DefaultColumnCapabilities => new ColumnCapabilities()
        {
            Capabilities = new ColumnCapabilitiesDefinition(null),
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

            return new ColumnCapabilities(new ColumnCapabilitiesDefinition(filterFunctions)
            {
                QueryAlias = propertyAlias,
                IsChoice = isChoice
            });
        }

        public bool Equals(ColumnCapabilities other)
        {
            return Enumerable.SequenceEqual((IEnumerable<KeyValuePair<string, ColumnCapabilitiesBase>>)this._childColumnsCapabilities ?? Array.Empty<KeyValuePair<string, ColumnCapabilitiesBase>>(), (IEnumerable<KeyValuePair<string, ColumnCapabilitiesBase>>)other._childColumnsCapabilities ?? Array.Empty<KeyValuePair<string, ColumnCapabilitiesBase>>()) &&
                   this.Capabilities.Equals(other.Capabilities);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnCapabilities);
        }        

        public override int GetHashCode()
        {
            int h = Capabilities.GetHashCode();

            if (_childColumnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase> kvp in _childColumnsCapabilities)
                {
                    h = Hashing.CombineHash(h, kvp.Key.GetHashCode());
                    h = Hashing.CombineHash(h, kvp.Value.GetHashCode());                    
                }
            }

            return h;
        }
    }
}
