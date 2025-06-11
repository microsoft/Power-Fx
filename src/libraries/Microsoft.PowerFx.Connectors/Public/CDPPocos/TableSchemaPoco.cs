// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    public class TableSchemaPoco
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "array";

        [JsonPropertyName("items")]
        public ItemsPoco Items { get; set; }
    }

    public class ItemsPoco
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        // need required flag.
        [JsonPropertyName("properties")]
        public Dictionary<string, ColumnInfoPoco> Properties { get; set; }
    }

    public class ColumnInfoPoco
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // $$$ "integer", "string",

        // What sorting capabilities?
        // "asc,desc"
        [JsonPropertyName("x-ms-sort")]
        public string Sort { get; set; }

        // What filter capabilities?
        [JsonPropertyName("x-ms-capabilities")]
        public ColumnCapabilitiesPoco Capabilities { get; set; }
    }
}
