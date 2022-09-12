// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Core
{
    internal class FormulaTypeSchema
    {
        /// <summary>
        /// Represents the type of this item.
        /// </summary>
        public SchemaTypeName Type { get; set; }

        // Optional, description for the type
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }        

        // Optional, help link for the type
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Help { get; set; }

        /// <summary>
        /// Optional. For Records and Tables, contains the list of fields.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, FormulaTypeSchema> Fields { get; set; }
    }
    
    [JsonConverter(typeof(SchemaTypeNameConverter))]
    public readonly struct SchemaTypeName
    {
        public string Type { get; init; }

        public bool IsTable { get; init; }
    }
}
