// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiFormUrlEncoder : FormulaValueSerializer
    {
        private readonly StringBuilder _writer;
        private readonly Stack<(string, int)> _stack;
        private readonly CancellationToken _cancellationToken;
        private bool _wrotePropertyName;

        public OpenApiFormUrlEncoder(IConvertToUTC utcConverter, bool schemaLessBody, CancellationToken cancellationToken)
            : base(utcConverter, schemaLessBody)
        {
            _writer = new StringBuilder(1024);
            _stack = new Stack<(string, int)>();
            _cancellationToken = cancellationToken;
        }

        protected override void StartArray(string name = null)
        {
            _stack.Push((name, -1));
        }

        protected override void StartArrayElement(string name)
        {
            var (existingName, currentIndex) = _stack.Pop();
            _stack.Push((existingName, currentIndex + 1));
        }

        protected override void EndArray()
        {
            _stack.Pop();
        }

        protected override void StartObject(string prefix = null)
        {
            _stack.Push((prefix, -2));
        }

        protected override void EndObject(string name = null)
        {
            _stack.Pop();
        }

        internal override string GetResult()
        {
            return _writer.ToString();
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            WritePropertyName(null);
            _writer.Append(booleanValue ? "true" : "false");
            _wrotePropertyName = false;
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            WritePropertyName(null);
            _writer.Append(Uri.EscapeDataString(dateTimeValue.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture)));
            _wrotePropertyName = false;
        }

        protected override void WriteDateTimeValueNoTimeZone(DateTime dateTimeValue)
        {
            WritePropertyName(null);
            _writer.Append(Uri.EscapeDataString(dateTimeValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture)));
            _wrotePropertyName = false;
        }

        protected override void WriteDateValue(DateTime dateValue)
        {
            _writer.Append(Uri.EscapeDataString(dateValue.Date.ToString("o", CultureInfo.InvariantCulture).Substring(0, 10)));
            _wrotePropertyName = false;
        }

        protected override void WriteNullValue()
        {
            WritePropertyName(null);
            _wrotePropertyName = false;
        }

        protected override void WriteNumberValue(double numberValue)
        {
            WritePropertyName(null);
            _writer.Append(numberValue);
            _wrotePropertyName = false;
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            WritePropertyName(null);
            _writer.Append(decimalValue);
            _wrotePropertyName = false;
        }

        protected override void WritePropertyName(string name)
        {
            if (!_wrotePropertyName)
            {
                _wrotePropertyName = true;
                AddSeparator();
                var prefix = GetPrefix();

                if (!string.IsNullOrEmpty(prefix))
                {
                    _writer.Append(prefix);

                    if (!string.IsNullOrEmpty(name))
                    {
                        _writer.Append('.');
                    }
                }

                if (!string.IsNullOrEmpty(name))
                {
                    _writer.Append(Uri.EscapeDataString(name));
                }

                _writer.Append('=');
            }
        }

        protected override void WriteStringValue(string stringValue)
        {
            WritePropertyName(null);
            _writer.Append(Uri.EscapeDataString(stringValue));
            _wrotePropertyName = false;
        }

        private void AddSeparator()
        {
            if (_writer.Length > 0)
            {
                _writer.Append('&');
            }
        }

        private string GetPrefix()
        {
            // only return the indicators, not the array index [0], [1], ... as per current serialization standard
            return string.Join(".", _stack.Select(e => e.Item1 ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)));
        }

        internal override void StartSerialization(string refId)
        {
            // Do nothing
        }

        internal override void EndSerialization()
        {
            // Do nothing
        }

        protected override Task WriteBlobValueAsync(BlobValue blobValue)
        {
            return Task.FromException(new NotImplementedException());
        }
    }
}
