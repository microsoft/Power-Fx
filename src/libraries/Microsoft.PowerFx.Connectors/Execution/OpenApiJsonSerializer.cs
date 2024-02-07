// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiJsonSerializer : FormulaValueSerializer, IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly Utf8JsonWriter _writer;
        private bool _topPropertyWritten = false;
        private bool _wasDisposed;
        private readonly CancellationToken _cancellationToken;

        public OpenApiJsonSerializer(IConvertToUTC utcConverter, bool schemaLessBody, CancellationToken cancellationToken)
            : base(utcConverter, schemaLessBody)
        {
            _stream = new MemoryStream();
            _writer = new Utf8JsonWriter(_stream, new JsonWriterOptions());
            _cancellationToken = cancellationToken;
        }

        internal override string GetResult()
        {
            _writer.Flush();
            return Encoding.UTF8.GetString(_stream.ToArray());
        }

        protected override void WritePropertyName(string name)
        {
            if (!_schemaLessBody || _topPropertyWritten)
            {
                _topPropertyWritten = true;
                _writer.WritePropertyName(name);
            }
        }

        protected override void WriteNullValue()
        {
            _writer.WriteNullValue();
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _writer.WriteNumberValue(numberValue);
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            _writer.WriteNumberValue(decimalValue);
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            _writer.WriteBooleanValue(booleanValue);
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            // ISO 8601                        
            _writer.WriteStringValue(dateTimeValue.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateTimeValueNoTimeZone(DateTime dateTimeValue)
        {
            // ISO 8601                        
            _writer.WriteStringValue(dateTimeValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateValue(DateTime dateValue)
        {
            _writer.WriteStringValue(dateValue.Date.ToString("o", CultureInfo.InvariantCulture).AsSpan(0, 10));
        }

        protected override void WriteStringValue(string stringValue)
        {
            _writer.WriteStringValue(stringValue);
        }

        protected override async void WriteBlobValue(BlobValue blobValue)
        {
            _writer.WriteBase64StringValue(await blobValue.GetAsByteArrayAsync(_cancellationToken).ConfigureAwait(false));
        }

        protected override void StartObject(string name = null)
        {
            if (string.IsNullOrEmpty(name) || (_schemaLessBody && !_topPropertyWritten))
            {
                _topPropertyWritten = true;
                _writer.WriteStartObject();
            }
            else
            {
                _writer.WriteStartObject(name);
            }
        }

        protected override void EndObject(string name = null)
        {
            if (!_schemaLessBody || _topPropertyWritten)
            {
                _writer.WriteEndObject();
            }
        }

        protected override void StartArray(string name = null)
        {
            if (string.IsNullOrEmpty(name) || (_schemaLessBody && !_topPropertyWritten))
            {
                _topPropertyWritten = true;
                _writer.WriteStartArray();
            }
            else
            {
                _writer.WriteStartArray(name);
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

        internal override void StartSerialization(string refId)
        {
            if (!_schemaLessBody)
            {
                StartObject();
            }
        }

        internal override void EndSerialization()
        {
            if (!_schemaLessBody)
            {
                EndObject();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_wasDisposed)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                    _stream?.Dispose();
                }

                _wasDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
