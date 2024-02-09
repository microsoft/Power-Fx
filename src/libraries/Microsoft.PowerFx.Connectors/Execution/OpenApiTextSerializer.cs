// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiTextSerializer : FormulaValueSerializer
    {
        private readonly StringBuilder _writer;
        private readonly CancellationToken _cancellationToken;

        public OpenApiTextSerializer(IConvertToUTC utcConverter, bool schemaLessBody, CancellationToken cancellationToken)
            : base(utcConverter, schemaLessBody)
        {
            _writer = new StringBuilder(1024);
            _cancellationToken = cancellationToken;
        }

        protected override void EndArray()
        {
        }

        protected override void EndObject(string name = null)
        {
        }

        protected override void StartArray(string name = null)
        {
        }

        protected override void StartArrayElement(string name)
        {
        }

        protected override void StartObject(string name = null)
        {
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            _writer.Append(booleanValue ? "true" : "false");
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            _writer.Append(dateTimeValue.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateTimeValueNoTimeZone(DateTime dateTimeValue)
        {
            _writer.Append(dateTimeValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateValue(DateTime dateValue)
        {
            _writer.Append(dateValue.Date.ToString("o", CultureInfo.InvariantCulture).Substring(0, 10));
        }

        protected override void WriteNullValue()
        {
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _writer.Append(numberValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            _writer.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WritePropertyName(string name)
        {
        }

        protected override void WriteStringValue(string stringValue)
        {
            _writer.Append(stringValue);
        }

        protected override async Task WriteBlobValueAsync(BlobValue blobValue)
        {            
            _writer.Append(await blobValue.GetAsStringAsync(null, _cancellationToken).ConfigureAwait(false));
        }

        internal override string GetResult()
        {
            return _writer.ToString();
        }

        internal override void StartSerialization(string refId)
        {
        }

        internal override void EndSerialization()
        {
        }
    }
}
