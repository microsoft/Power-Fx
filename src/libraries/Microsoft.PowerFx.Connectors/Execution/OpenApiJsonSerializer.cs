// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiJsonSerializer : FormulaValueSerializer
    {        
        private readonly MemoryStream _stream;
        private readonly Utf8JsonWriter _writer;        

        internal OpenApiJsonSerializer()
            : base()
        {
            _stream = new MemoryStream();
            _writer = new Utf8JsonWriter(_stream, new JsonWriterOptions());
        }

        protected override string GetResult()
        {
            _writer.Flush();
            return Encoding.UTF8.GetString(_stream.ToArray());
        }

        protected override void WritePropertyName(string name)
        {
            _writer.WritePropertyName(name);
        }

        protected override void WriteNullValue()
        {
            _writer.WriteNullValue();
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _writer.WriteNumberValue(numberValue);
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            _writer.WriteBooleanValue(booleanValue);
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            // ISO 8601
            _writer.WriteStringValue(dateTimeValue.ToString("o", CultureInfo.InvariantCulture));
        }

        protected override void WriteStringValue(string stringValue)
        {
            _writer.WriteStringValue(stringValue);
        }

        protected override void StartObject(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                _writer.WriteStartObject();
            }
            else
            {
                _writer.WriteStartObject(JsonEncodedText.Encode(name));
            }
        }

        protected override void EndObject()
        {
            _writer.WriteEndObject();
        }

        protected override void StartArray(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                _writer.WriteStartArray();
            }
            else
            {
                _writer.WriteStartArray(JsonEncodedText.Encode(name));
            }
        }

        protected override void EndArray()
        {
            _writer.WriteEndArray();
        }

        protected override void StartArrayElement(string name)
        {
            // Do nothing
        }
    }
}
