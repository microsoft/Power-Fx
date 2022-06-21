// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiXmlSerializer : FormulaValueSerializer
    {
        private XElement _root;
        private readonly Stack<XElement> _stack;
        private string _rootName;

        public OpenApiXmlSerializer()
            : base()
        {            
            _stack = new Stack<XElement>();
        }

        protected override void EndArray(string name = null)
        {
            EndObject();
        }

        internal override string GetResult()
        {
            return _root.ToString(SaveOptions.DisableFormatting);
        }

        protected override void StartArray(string name = null)
        {
            var xe = new XElement(name);
            _stack.Push(xe);
        }

        protected override void StartArrayElement(string name)
        {
            StartObject("e");
        }

        protected override void StartObject(string name = null)
        {
            var xe = new XElement(name ?? _rootName);

            if (_stack.Count > 0)
            {
                _stack.Peek().Add(xe);
            }

            _stack.Push(xe);
        }

        protected override void EndObject(string name = null)
        {
            var xe = _stack.Pop();

            if (_stack.Count == 0)
            {
                _root = xe;
            }
            else
            {
                _stack.Peek().Add(xe);
            }
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            _stack.Pop().Value = booleanValue.ToString();
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {
            _stack.Pop().Value = dateTimeValue.ToString("o", CultureInfo.InvariantCulture);
        }

        protected override void WriteNullValue()
        {
            // Do Nothing;
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _stack.Pop().Value = numberValue.ToString();
        }

        protected override void WritePropertyName(string name)
        {
            StartObject(name);
        }

        protected override void WriteStringValue(string stringValue)
{
            _stack.Pop().Value = stringValue;
        }

        internal override void StartSerialization(string refId)
        {
            _rootName = refId ?? "Xml";
            StartObject(_rootName);
            _root = _stack.Peek();
        }

        internal override void EndSerialization(string refId)
        {
            // Do nothing
        }
    }
}
