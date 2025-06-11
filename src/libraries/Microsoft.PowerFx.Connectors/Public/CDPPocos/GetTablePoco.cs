// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    public class GetTablePoco
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("x-ms-permission")]
        public string Permissions { get; set; } // read-write

        // $$$ Fill in rest of stuff here...
        [JsonPropertyName("x-ms-capabilities")]
        public CapabilitiesPoco Capabilities { get; set; }

        [JsonPropertyName("schema")]
        public TableSchemaPoco Schema { get; set; }
    }
}
