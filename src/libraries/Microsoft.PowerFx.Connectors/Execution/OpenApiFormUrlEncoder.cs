// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiFormUrlEncoder : FormulaValueSerializer
    {
        private readonly StringBuilder _writer;
        private readonly Stack<string> _stack;
        private int _arrayIndex;

        internal OpenApiFormUrlEncoder()
        {
            _writer = new StringBuilder(1024);    
            _stack = new Stack<string>();       
            _arrayIndex = 0;
        }

        internal override void StartArray(string name = null)
        {
            WritePropertyName(name);
            _arrayIndex = 0;
        }

        internal override void StartArrayElement(string name)
        {
            if (_arrayIndex++ != 0)
            {
                WritePropertyName(name);
            }
        }

        internal override void EndArray()
        {
            // Do nothing
        }

        internal override void StartObject(string prefix = null)
        {
            _stack.Push(prefix);
        }

        internal override void EndObject()
        {
            _stack.Pop();
        }

        internal override string GetResult()
        {
            return _writer.ToString();
        }

        internal override void WriteBooleanValue(bool booleanValue)
        {
            _writer.Append(booleanValue ? "true" : "false");
        }

        internal override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            _writer.Append(dateTimeValue.ToString("o", CultureInfo.InvariantCulture));
        }

        internal override void WriteNullValue()
        {
            // Do nothing
        }

        internal override void WriteNumberValue(double numberValue)
        {
            _writer.Append(numberValue);
        }

        internal override void WritePropertyName(string name)
        {
            AddSeparator();
            var prefix = GetPrefix();

            if (!string.IsNullOrEmpty(prefix))
            {
                _writer.Append(prefix);
                _writer.Append('.');
            }

            _writer.Append(name);
            _writer.Append('=');
        }

        internal override void WriteStringValue(string stringValue)
        {
            _writer.Append(stringValue);
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
    }
}
