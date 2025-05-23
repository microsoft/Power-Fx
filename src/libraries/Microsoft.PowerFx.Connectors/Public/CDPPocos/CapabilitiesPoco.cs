// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Connectors
{
    public class CapabilitiesPoco
    {
        [JsonPropertyName("sortRestrictions")]
        public Sort SortRestrictions { get; set; }

        [JsonPropertyName("filterRestrictions")]
        public Filter FilterRestrictions { get; set; }

        [JsonPropertyName("isOnlyServerPagable")]
        public bool IsOnlyServerPagable { get; set; }

        // "top", "skiptoken"
        [JsonPropertyName("serverPagingOptions")]
        public string[] ServerPagingOptions { get; set; }

        // and,or,eq, etc...
        // DelegationOperator
        [JsonPropertyName("filterFunctionSupport")]
        public string[] FilterFunctionSupport { get; set; }

        public CapabilitiesPoco SetOps(IEnumerable<DelegationOperator> ops)
        {
            this.FilterFunctionSupport = ops.ToStr().ToArray();
            return this;
        }

        public IEnumerable<DelegationOperator> FilterFunctionSupportOps() => Utilities.ToDelegationOp(FilterFunctionSupport);

        // 3
        [JsonPropertyName("odataVersion")]
        public int OdataVersion { get; set; }
    }

    public class Filter
    {
        [JsonPropertyName("filterable")]
        public bool Filterable { get; set; }

        [JsonPropertyName("nonFilterableProperties")]
        public string[] NonFilterableProperties { get; set; }
    }

    public class Sort
    {
        [JsonPropertyName("sortable")]
        public bool Sortable { get; set; }

        [JsonPropertyName("unsortableProperties")]
        public string[] UnsortableProperties { get; set; }
    }
}
