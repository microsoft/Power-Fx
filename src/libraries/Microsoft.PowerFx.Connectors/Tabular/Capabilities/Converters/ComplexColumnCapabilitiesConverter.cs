// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal class ComplexColumnCapabilitiesConverter : JsonConverter<ComplexColumnCapabilities>
    {
        public override ComplexColumnCapabilities Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Write Only support
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ComplexColumnCapabilities value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var pair in value._childColumnsCapabilities)
            {
                writer.WritePropertyName(pair.Key);
                JsonSerializer.Serialize(writer, pair.Value, pair.Value.GetType(), options);
            }

            writer.WriteEndObject();
        }
    }
}
