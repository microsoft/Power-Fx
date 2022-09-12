// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core
{
    internal sealed class FormulaTypeSchema
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
        public Dictionary<string, FormulaTypeSchema> Fields { get; set; }

        public override bool Equals(object o)
        {
            return o is FormulaTypeSchema other && 
                Type.Equals(other.Type) &&
                Description == other.Description &&
                Help == other.Help && 
                Fields?.Count() == other.Fields?.Count() &&
                (Fields?.All(
                     (thisKvp) => other.Fields.TryGetValue(thisKvp.Key, out var otherValue) &&
                     (thisKvp.Value?.Equals(otherValue) ?? otherValue == null)) ?? true);
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(
                Type.GetHashCode(),
                Description.GetHashCode(),
                Help.GetHashCode(),
                Fields.GetHashCode());
        }
    }
    
    [JsonConverter(typeof(SchemaTypeNameConverter))]
    public readonly struct SchemaTypeName
    {
        public string Type { get; init; }

        public bool IsTable { get; init; }
    }
}
