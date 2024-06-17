// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    internal class AbstractTypeConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Write Only support
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
