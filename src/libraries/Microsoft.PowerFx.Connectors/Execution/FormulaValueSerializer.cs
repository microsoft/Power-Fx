// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal abstract class FormulaValueSerializer
    {
        protected abstract string GetResult();

        protected abstract void StartSerialization(OpenApiSchema schema);

        protected abstract void StartObject(string name = null);

        protected abstract void EndObject();

        protected abstract void StartArray(string name = null);

        protected abstract void StartArrayElement(string name);

        protected abstract void EndArray();

        protected abstract void WritePropertyName(string name);

        protected abstract void WriteNullValue();

        protected abstract void WriteNumberValue(double numberValue);

        protected abstract void WriteStringValue(string stringValue);

        protected abstract void WriteBooleanValue(bool booleanValue);

        protected abstract void WriteDateTimeValue(DateTime dateTimeValue);        

        internal string Serialize(OpenApiSchema schema, IEnumerable<NamedValue> fields)
        {
            StartSerialization(schema);
            WriteObject(null, schema, fields);           
            return GetResult();
        }

        private void WriteObject(string objectName, OpenApiSchema schema, IEnumerable<NamedValue> fields)
        {
            StartObject(objectName);

            foreach (var property in schema.Properties)
            {
                var namedValue = fields.FirstOrDefault(nv => nv.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase));

                if (namedValue == null)
                {
                    throw new NotImplementedException($"Missing property {property.Key}, object is too complex or not supported");
                }

                WriteProperty(property.Key, property.Value, namedValue.Value);
            }

            EndObject();
        }

        private void WriteProperty(string propertyName, OpenApiSchema propertySchema, FormulaValue fv)
        {
            switch (propertySchema.Type)
            {
                case "array":                    
                    StartArray(propertyName);
                    
                    if (fv is not TableValue tv)
                    {
                        throw new ArgumentException($"Type mismatch, expecting an array for {propertyName} and {fv.ToObject()} is {fv.GetType().FullName}");
                    }

                    foreach (var item in tv.Rows)
                    {
                        if (item.Value is not RecordValue rva)
                        {
                            throw new ArgumentException($"Invalid type in array, expecting a RecordValue for {propertyName}, row type is {item.GetType().FullName}");
                        }

                        Contract.Assert(rva.Fields.Count() == 1);

                        StartArrayElement(propertyName);
                        WriteValue(rva.Fields.First().Value);
                    }
                    
                    EndArray();
                    break;

                case "null":
                case "number":
                case "boolean":
                case "integer":
                case "string":
                    WritePropertyValue(propertyName, fv);
                    break;

                case "object":                    
                    if (fv is not RecordValue rvo)
                    {
                        throw new ArgumentException($"Invalid type in object, expecting a RecordValue for property {propertyName}, object type is {fv.GetType().FullName}");
                    }
                    
                    WriteObject(propertyName, propertySchema, rvo.Fields);                    
                    break;

                default:
                    throw new NotImplementedException($"Not support property type {propertySchema.Type} for property {propertyName}");
            }
        }

        private void WritePropertyValue(string name, FormulaValue value)
        {            
            WritePropertyName(name);
            WriteValue(value);
        }

        private void WriteValue(FormulaValue value)
        {
            if (value == null || value is BlankValue)
            {
                WriteNullValue();
            }
            else if (value is NumberValue numberValue)
            {
                WriteNumberValue(numberValue.Value);
            }
            else if (value is StringValue stringValue)
            {
                WriteStringValue(stringValue.Value);
            }
            else if (value is BooleanValue booleanValue)
            {
                WriteBooleanValue(booleanValue.Value);                
            }            
            else if (value is PrimitiveValue<DateTime> dt) 
            {
                // DateTimeValue and DateValue
                WriteDateTimeValue(dt.Value);
            }
            else
            {
                throw new NotImplementedException($"Not supported type {value.GetType().FullName} for value");
            }
        }
    }
}
