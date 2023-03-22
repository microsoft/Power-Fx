// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiFormUrlEncoder : FormulaValueSerializer
    {
        private readonly StringBuilder _writer;
        private readonly Stack<string> _stack;
        private readonly Stack<int> _arrayIndex;

        public OpenApiFormUrlEncoder(bool schemaLessBody)
            : base(schemaLessBody)
        {
            _writer = new StringBuilder(1024);    
            _stack = new Stack<string>();
            _arrayIndex = new Stack<int>();
        }

        protected override void StartArray(string name = null)
        {
            WritePropertyName(name);
            _arrayIndex.Push(0);
        }

        protected override void StartArrayElement(string name)
        {
            var currentIndex = _arrayIndex.Pop();
            if (currentIndex++ != 0)
            {
                WritePropertyName(name);
            }

            _arrayIndex.Push(currentIndex);
        }

        protected override void EndArray()
        {
            _arrayIndex.Pop();
        }

        protected override void StartObject(string prefix = null)
        {
            _stack.Push(prefix);
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
            _writer.Append(booleanValue ? "true" : "false");
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            _writer.Append(HttpUtility.UrlEncode(dateTimeValue.ToString("o", CultureInfo.InvariantCulture)));
        }

        protected override void WriteNullValue()
        {
            // Do nothing
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _writer.Append(numberValue);
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            _writer.Append(decimalValue);
        }

        protected override void WritePropertyName(string name)
        {
            AddSeparator();
            var prefix = GetPrefix();

            if (!string.IsNullOrEmpty(prefix))
            {
                _writer.Append(HttpUtility.UrlEncode(prefix));
                _writer.Append('.');
            }

            _writer.Append(HttpUtility.UrlEncode(name));
            _writer.Append('=');
        }

        protected override void WriteStringValue(string stringValue)
        {
            _writer.Append(HttpUtility.UrlEncode(stringValue));
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
            return string.Join(".", _stack.Where(e => !string.IsNullOrEmpty(e)));
        }

        internal override void StartSerialization(string refId)
        {
            // Do nothing
        }

        internal override void EndSerialization()
        {
            // Do nothing
        }
    }
}
