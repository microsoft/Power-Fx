// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Linq;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiJsonSerializer
    {
        private readonly Dictionary<string, object> _dic;
        private readonly MemoryStream _stream;
        private readonly Utf8JsonWriter _writer;
        private readonly OpenApiSchema _schema;

        internal OpenApiJsonSerializer(OpenApiSchema schema, Dictionary<string, object> dictionary)
        {
            _schema = schema;
            _dic = dictionary;
            _stream = new MemoryStream();
            _writer = new Utf8JsonWriter(_stream, new JsonWriterOptions());
        }

        internal string ToJson()
        {
            _writer.WriteStartObject();
            foreach (var prop in _schema.Properties)
            {
                WriteProperty(prop);
            }
            _writer.WriteEndObject();
            _writer.Flush();
            var json = Encoding.UTF8.GetString(_stream.ToArray());
            return json;
        }

        private void WriteProperty(KeyValuePair<string, OpenApiSchema> property, object outerValue = null)
        {
            if (!_dic.TryGetValue(property.Key, out var value))
            {
                if (outerValue != null && outerValue is ExpandoObject eo)
                {
                    value = eo.FirstOrDefault(kvp => kvp.Key == property.Key);

                    if (value != null && value is KeyValuePair<string, object> kvp)
                    {
                        value = kvp.Value;
                    }
                }
            }

            if (value == null)
            {
                throw new NotImplementedException($"Missing property {property.Key}, object is too complex or not supported");
            }

            switch (property.Value.Type)
            {
                case "array":
                    _writer.WriteStartArray(JsonEncodedText.Encode(property.Key));
                    if (value is not IEnumerable @enum)
                    {
                        throw new ArgumentException($"Type mismatch, expecting an array for {property.Key} and {value} is {value.GetType().FullName}");
                    }
                    foreach (var item in @enum)
                    {
                        WriteValue(item);
                    }
                    _writer.WriteEndArray();
                    break;

                case "null":
                case "number":
                case "boolean":
                case "integer":
                case "string":
                    WriteProperty(property.Key, value);
                    break;

                case "object":
                    _writer.WriteStartObject(property.Key);
                    foreach (var prop in property.Value.Properties)
                    {
                        WriteProperty(prop, value);
                    }
                    _writer.WriteEndObject();
                    break;

                default:
                    throw new NotImplementedException($"Not support property type {property.Value.Type} for property {property.Key}");
            }
        }

        private void WriteProperty(string name, object value)
        {
            if (value == null)
            {
                _writer.WriteNull(name);
            }
            else if (value is string s)
            {
                _writer.WriteString(name, s);
            }
            else if (value is bool b)
            {
                _writer.WriteBoolean(name, b);
            }
            else if (value is decimal dec)
            {
                _writer.WriteNumber(name, dec);
            }
            else if (value is double dbl)
            {
                _writer.WriteNumber(name, dbl);
            }
            else if (value is float flt)
            {
                _writer.WriteNumber(name, flt);
            }
            else if (value is int i)
            {
                _writer.WriteNumber(name, i);
            }
            else if (value is long lng)
            {
                _writer.WriteNumber(name, lng);
            }
            else if (value is uint ui)
            {
                _writer.WriteNumber(name, ui);
            }
            else if (value is ulong ul)
            {
                _writer.WriteNumber(name, ul);
            }
            else
            {
                throw new NotImplementedException($"Not supported type {value.GetType().FullName} for value {value}, property {name}");
            }
        }

        private void WriteValue(object value)
        {
            if (value == null)
            {
                _writer.WriteNullValue();
            }
            else if (value is bool b)
            {
                _writer.WriteBooleanValue(b);
            }
            else if (value is string s)
            {
                _writer.WriteStringValue(s);
            }
            else if (value is decimal dec)
            {
                _writer.WriteNumberValue(dec);
            }
            else if (value is double dbl)
            {
                _writer.WriteNumberValue(dbl);
            }
            else if (value is float flt)
            {
                _writer.WriteNumberValue(flt);
            }
            else if (value is int i)
            {
                _writer.WriteNumberValue(i);
            }
            else if (value is long lng)
            {
                _writer.WriteNumberValue(lng);
            }
            else if (value is uint ui)
            {
                _writer.WriteNumberValue(ui);
            }
            else if (value is ulong ul)
            {
                _writer.WriteNumberValue(ul);
            }
            else
            {
                throw new NotImplementedException($"Not supported type {value.GetType().FullName} for value {value}");
            }
        }
    }
}
