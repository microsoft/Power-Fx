// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private readonly DefinedTypeSymbolTable _definedTypes;

        private static readonly ISet<string> _jsonIgnoreWhenNull = new HashSet<string>()
        {
            "Description",
            "Help",
            "Fields",
        };

        public FormulaTypeJsonConverter(DefinedTypeSymbolTable definedTypes)
        {
            _definedTypes = definedTypes;
        }

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, options);
            return schemaPoco.ToFormulaType(_definedTypes);
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = value.ToSchema(_definedTypes);

            writer.WriteStartObject();

            using (var document = JsonDocument.Parse(JsonSerializer.Serialize(schemaPoco, options)))
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!(_jsonIgnoreWhenNull.Contains(property.Name) && property.Value.ValueKind == JsonValueKind.Null))
                    {
                        property.WriteTo(writer);
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}
