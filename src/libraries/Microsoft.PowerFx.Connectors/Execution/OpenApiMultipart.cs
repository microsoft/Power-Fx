// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "False positive")]
    internal class OpenApiMultipart : FormulaValueSerializer
    {
        internal const string Boundary = "---------- PowerFxBoundary-3BE81CA086E642158";

        // disposable object
        // caller of GetHttpContent() is responsible to dispose it
        private readonly MultipartFormDataContent _formData;

        private readonly CancellationToken _cancellationToken;

        private string _propName;

        private string _arrayName;

        private StringBuilder _array;

        private bool _arrayStarted;        

        public OpenApiMultipart(IConvertToUTC utcConverter, bool schemaLessBody, CancellationToken cancellationToken)
            : base(utcConverter, schemaLessBody)
        {
            _formData = new MultipartFormDataContent(Boundary);

            _cancellationToken = cancellationToken;
            _propName = null;
            _arrayName = null;
            _array = null;
            _arrayStarted = false;
        }

        internal override bool GeneratesHttpContent => true;

        protected override void EndArray()
        {
            _propName = _arrayName;
            _arrayName = null;
            _arrayStarted = false;

            WriteStringValue(_array.ToString());
        }

        protected override void EndObject(string name = null)
        {
            throw new NotImplementedException();
        }

        protected override void StartArray(string name = null)
        {
            _arrayName = name;
            _array = new StringBuilder();
            _arrayStarted = false;
        }

        protected override void StartArrayElement(string name)
        {
        }

        protected override void StartObject(string name = null)
        {
            throw new NotImplementedException();
        }

        protected override async Task WriteBlobValueAsync(BlobValue blobValue)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            _formData.Add(new ByteArrayContent(await blobValue.GetAsByteArrayAsync(_cancellationToken).ConfigureAwait(false)), _propName, _propName);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            WriteStringValue(booleanValue ? "true" : "false");
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            WriteStringValue(dateTimeValue.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateTimeValueNoTimeZone(DateTime dateTimeValue)
        {
            WriteStringValue(dateTimeValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
        }

        protected override void WriteDateValue(DateTime dateValue)
        {
            WriteStringValue(dateValue.Date.ToString("o", CultureInfo.InvariantCulture).Substring(0, 10));
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            WriteStringValue(decimalValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WriteNullValue()
        {
            throw new NotImplementedException();
        }

        protected override void WriteNumberValue(double numberValue)
        {
            WriteStringValue(numberValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WritePropertyName(string name)
        {
            _propName = name;
        }

        protected override void WriteStringValue(string stringValue)
        {
            if (_propName != null)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                _formData.Add(new StringContent(stringValue, Encoding.UTF8), _propName);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
            else if (_arrayName != null)
            {
                if (_arrayStarted)
                {
                    _array.Append(',');
                }

                _array.Append(stringValue);
                _arrayStarted = true;
            }
        }

        internal override void EndSerialization()
        {
        }

        internal override HttpContent GetHttpContent()
        {
            return _formData;
        }

        internal override void StartSerialization(string referenceId)
        {
        }
    }
}
