using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Linq;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiFormUrlEncoder
    {
        private readonly Dictionary<string, object> _dic;        
        private readonly StringBuilder _writer;
        private readonly OpenApiSchema _schema;

        internal OpenApiFormUrlEncoder(OpenApiSchema schema, Dictionary<string, object> dictionary)
        {
            _schema = schema;
            _dic = dictionary;
            _writer = new StringBuilder(1024);
        }

        internal string ToUrlEncodedForm()
        {            
            foreach (var prop in _schema.Properties)
            {
                WriteProperty(prop);
            }

            return _writer.ToString();
        }

        private void AddSeparator()
        {
            if (_writer.Length > 0)
            {
                _writer.Append('&');
            }
        }

        private void WriteProperty(KeyValuePair<string, OpenApiSchema> property, object outerValue = null, string prefix = null)
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
                    if (value is not IEnumerable @enum)
                    {
                        throw new ArgumentException($"Type mismatch, excepcting an array for {property.Key} and {value} is {value.GetType().FullName}");
                    }
                    foreach (var item in @enum)
                    {
                        AddSeparator();
                        if (prefix != null)
                        {
                            _writer.Append(prefix);
                            _writer.Append('.');
                        }
                        _writer.Append(property.Key);
                        _writer.Append('=');
                        WriteValue(item, property.Key);
                    }                    
                    break;

                case "null":
                case "number":
                case "boolean":
                case "integer":
                case "string":
                    WriteProperty(property.Key, value, prefix);
                    break;

                case "object":
                    var innerPrefix = prefix == null ? property.Key : $"{prefix}.{property.Key}";
                    foreach (var prop in property.Value.Properties)
                    {                        
                        WriteProperty(prop, value, innerPrefix);
                    }                   
                    break;

                default:
                    throw new NotImplementedException($"Not support property type {property.Value.Type} for property {property.Key}");
            }
        }

        private void WriteProperty(string name, object value, string prefix)
        {
            AddSeparator();
            if (prefix != null)
            {
                _writer.Append(prefix);
                _writer.Append('.');
            }
            _writer.Append(name);
            _writer.Append('=');
            
            WriteValue(value, name);            
        }

        private void WriteValue(object value, string name)
        {
            if (value == null)
            {
                // do nothing
            }
            else if (value is string s)
            {
                _writer.Append(s);
            }
            else if (value is bool b)
            {
                _writer.Append(b);
            }
            else if (value is decimal dec)
            {
                _writer.Append(dec);
            }
            else if (value is double dbl)
            {
                _writer.Append(dbl);
            }
            else if (value is float flt)
            {
                _writer.Append(flt);
            }
            else if (value is int i)
            {
                _writer.Append(i);
            }
            else if (value is long lng)
            {
                _writer.Append(lng);
            }
            else if (value is uint ui)
            {
                _writer.Append(ui);
            }
            else if (value is ulong ul)
            {
                _writer.Append(ul);
            }
            else if (value is byte by)
            {
                _writer.Append(by);
            }
            else if (value is sbyte sby)
            {
                _writer.Append(sby);
            }
            else if (value is char c)
            {
                _writer.Append(c);
            }
            else if (value is short sh)
            {
                _writer.Append(sh);
            }
            else if (value is ushort ush)
            {
                _writer.Append(ush);
            }
            else
            {
                throw new NotImplementedException($"Not supported type {value.GetType().FullName} for value {value}, property {name}");
            }
        }
    }
}
