// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class SchemaTypeNameConverter : JsonConverter<SchemaTypeName>
    {
        private readonly JsonSerializerOptions _options;

        public SchemaTypeNameConverter()
        {
            _options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _options.Converters.Add(new JsonStringEnumConverter());
        }

        public override SchemaTypeName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new SchemaTypeName() { Type = reader.GetString(), IsTable = false };
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                SkipComments(ref reader);

                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException();
                }
                
                var typeName = reader.GetString();

                if (!reader.Read())
                {
                    throw new JsonException();
                }

                SkipComments(ref reader);

                // Must be exactly one element in the array
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return new SchemaTypeName() { Type = typeName, IsTable = true };
                }
            }
                    
            throw new JsonException();
        }

        private static void SkipComments(ref Utf8JsonReader reader)
        {
            while (reader.Read() && reader.TokenType == JsonTokenType.Comment)
            {
                // skip the comments, do nothing
            }
        }

        public override void Write(Utf8JsonWriter writer, SchemaTypeName value, JsonSerializerOptions options)
        {
            if (value.IsTable)
            {
                writer.WriteStartArray();
                writer.WriteStringValue(value.Type);
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStringValue(value.Type);
            }
        }
    }
}
