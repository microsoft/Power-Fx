// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Types;
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

        internal static readonly DType DType_Table = new (DKind.Table);
        internal static readonly DType DType_Record = new (DKind.Record);

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
                    // array
                    AssertType(DType_Table, fv, propertyName);
                    StartArray(propertyName);
                         
                    foreach (var item in (fv as TableValue).Rows)
                    {
                        if (item.Value is not RecordValue rva)
                        {
                            throw new ArgumentException($"Invalid type in array, expecting a RecordValue for {propertyName}, row type is {item.GetType().FullName}");
                        }

                        if (rva.Fields.Count() != 1)
                        {
                            throw new ArgumentException($"Incompatible Table for supporting array, RecordValue has more than one column - propertyName {propertyName}, number of fields {rva.Fields.Count()}");
                        }

                        StartArrayElement(propertyName);
                        WriteValue(rva.Fields.First().Value);
                    }
                    
                    EndArray();
                    break;

                case "null":
                    // nullable
                    AssertType(DType.ObjNull, fv, propertyName);
                    WritePropertyValue(propertyName, fv);
                    break;

                case "number":
                    // float, double
                    AssertType(DType.Number, fv, propertyName);
                    WritePropertyValue(propertyName, fv);
                    break;

                case "boolean":
                    AssertType(DType.Boolean, fv, propertyName);
                    WritePropertyValue(propertyName, fv);
                    break;

                case "integer":
                    // int16, int32, int64
                    AssertType(DType.Number, fv, propertyName);
                    WritePropertyValue(propertyName, fv);
                    break;

                case "string":
                    // string, binary, date, date-time, password, byte (base64)
                    WritePropertyValue(propertyName, fv);
                    break;

                case "object":
                    // collection of property/value pairs
                    AssertType(DType_Record, fv, propertyName);                                        
                    WriteObject(propertyName, propertySchema, (fv as RecordValue).Fields);                    
                    break;

                default:
                    throw new NotImplementedException($"Not supported property type {propertySchema.Type} for property {propertyName}");
            }
        }

        private void AssertType(DType expectedType, FormulaValue fv, string propertyName)
        {
            if (fv == null || fv.Type == null)
            {
                throw new ArgumentException($"Missing valid FormulaValue for property {propertyName}");
            }

            var actualType = fv.Type._type;

            if (expectedType.Kind != actualType.Kind)
            {
                throw new ArgumentException($"Type mismatch for property {propertyName}, expected type {expectedType.Kind} and got {actualType.Kind}");
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
