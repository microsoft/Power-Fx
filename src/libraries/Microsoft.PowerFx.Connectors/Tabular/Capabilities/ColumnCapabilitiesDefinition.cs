// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

namespace Microsoft.PowerFx.Connectors
{
    internal sealed class ColumnCapabilitiesDefinition : IEquatable<ColumnCapabilitiesDefinition>
    {
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterFunctions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string> FilterFunctions { get; }

        // used in PowerApps-Client/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.PropertyQueryAlias)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        internal string QueryAlias { get; init; }

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SPIsChoice)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        internal bool? IsChoice { get; init; }
        
        public ColumnCapabilitiesDefinition(IEnumerable<string> filterFunction)
        {                   
            // ex: lt, le, eq, ne, gt, ge, and, or, not, contains, startswith, endswith, countdistinct, day, month, year, time
            FilterFunctions = filterFunction;           
        }

        public bool Equals(ColumnCapabilitiesDefinition other)
        {
            if (other == null)
            {
                return false;
            }

            return Enumerable.SequenceEqual(this.FilterFunctions ?? Array.Empty<string>(), other.FilterFunctions ?? Array.Empty<string>()) &&
                   this.QueryAlias == other.QueryAlias &&
                   this.IsChoice == other.IsChoice;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnCapabilitiesDefinition);
        }

        public override int GetHashCode()
        {
            int h = Hashing.CombineHash(QueryAlias.GetHashCode(), IsChoice?.GetHashCode() ?? 0);

            if (FilterFunctions != null)
            {
                foreach (var filterFunction in FilterFunctions)
                {
                    h = Hashing.CombineHash(h, filterFunction.GetHashCode());
                }
            }

            return h;
        }
    }
}
