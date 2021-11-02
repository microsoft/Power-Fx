// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.PowerFx.Tests
{
    // From: https://github.com/microsoft/PowerApps-Language-Tooling/blob/master/src/PAModel/Utility/JsonNormalizer.cs
    // Write out Json in a normalized sorted order. 
    // Orders properties, whitespace/indenting, etc. 
    public class JsonNormalizer
    {
        public static string Normalize(string jsonStr)
        {
            JsonElement je = JsonDocument.Parse(jsonStr).RootElement;
            return Normalize(je);
        }

        public static string Normalize(JsonElement je)
        {
            var ms = new MemoryStream();
            JsonWriterOptions opts = new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            using (var writer = new Utf8JsonWriter(ms, opts))
            {
                Write(je, writer);
            }

            var bytes = ms.ToArray();
            var str = Encoding.UTF8.GetString(bytes);
            return str;
        }

        private static void Write(JsonElement je, Utf8JsonWriter writer)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();

                    foreach (JsonProperty x in je.EnumerateObject().OrderBy(prop => prop.Name))
                    {
                        writer.WritePropertyName(x.Name);
                        Write(x.Value, writer);
                    }

                    writer.WriteEndObject();
                    break;

                // When normalizing... original msapp arrays can be in any order...
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (JsonElement x in je.EnumerateArray())
                    {
                        Write(x, writer);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.Number:
                    writer.WriteNumberValue(je.GetDouble());
                    break;

                case JsonValueKind.String:
                    // Escape the string 
                    writer.WriteStringValue(je.GetString());
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                default:
                    throw new NotImplementedException($"Kind: {je.ValueKind}");

            }
        }
    }
}
