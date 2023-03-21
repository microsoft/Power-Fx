// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal abstract class FormulaValueSerializer
    {
        internal abstract string GetResult();

        internal abstract void StartSerialization(string referenceId);

        internal abstract void EndSerialization();

        protected abstract void StartObject(string name = null);

        protected abstract void EndObject(string name = null);

        protected abstract void StartArray(string name = null);

        protected abstract void StartArrayElement(string name);

        protected abstract void EndArray();

        protected abstract void WritePropertyName(string name);

        protected abstract void WriteNullValue();

        protected abstract void WriteNumberValue(double numberValue);

        protected abstract void WriteDecimalValue(decimal decimalValue);

        protected abstract void WriteStringValue(string stringValue);

        protected abstract void WriteBooleanValue(bool booleanValue);

        protected abstract void WriteDateTimeValue(DateTime dateTimeValue);

        protected readonly bool _schemaLessBody;

        internal FormulaValueSerializer(bool schemaLessBody)
        {
            _schemaLessBody = schemaLessBody;
        }

        internal void SerializeValue(string paramName, OpenApiSchema schema, FormulaValue value)
        {
            WriteProperty(paramName, schema, value);
        }

        private void WriteObject(string objectName, OpenApiSchema schema, IEnumerable<NamedValue> fields)
        {
            StartObject(objectName);

            foreach (var property in schema.Properties)
            {
                var namedValue = fields.FirstOrDefault(nv => nv.Name.Equals(property.Key, StringComparison.OrdinalIgnoreCase));

                if (namedValue == null)
                {
                    if (property.Value.IsInternal())
                    {
                        continue;
                    }

                    throw new NotImplementedException($"Missing property {property.Key}, object is too complex or not supported");
                }

                WriteProperty(property.Key, property.Value, namedValue.Value);
            }

            if (!schema.Properties.Any() && fields.Any())
            {
                foreach (NamedValue nv in fields)
                {
                    WriteProperty(
                        nv.Name,
                        new OpenApiSchema()
                        {
                            Type = nv.Value.Type._type.Kind switch
                            {
                                DKind.Number => "number",
                                DKind.String => "string",
                                DKind.Boolean => "boolean",
                                DKind.Record => "object",
                                DKind.Table => "array",
                                DKind.ObjNull => "null",
                                _ => "unknown_dkind"
                            }
                        }, 
                        nv.Value);
                }
            }

            EndObject(objectName);
        }

        private void WriteProperty(string propertyName, OpenApiSchema propertySchema, FormulaValue fv)
        {
            if (propertySchema == null)
            {
                throw new ArgumentException($"Missing schema for property {propertyName}");
            }

            switch (propertySchema.Type)
            {
                case "array":
                    // array                    
                    StartArray(propertyName);

                    foreach (var item in (fv as TableValue).Rows)
                    {
                        var rva = item.Value;

                        if (rva.Fields.Count() != 1)
                        {
                            throw new ArgumentException($"Incompatible Table for supporting array, RecordValue has more than one column - propertyName {propertyName}, number of fields {rva.Fields.Count()}");
                        }

                        if (rva.Fields.First().Name != "Value")
                        {
                            throw new ArgumentException($"Incompatible Table for supporting array, RecordValue doesn't have 'Value' column - propertyName {propertyName}");
                        }

                        StartArrayElement(propertyName);
                        WriteValue(rva.Fields.First().Value);
                    }

                    EndArray();
                    break;

                case "null":
                    // nullable
                    throw new NotImplementedException($"null schema type not supported yet for property {propertyName}");

                case "number":
                    // float, double                    
                    WritePropertyName(propertyName);

                    if (fv is NumberValue numberValue)
                    {
                        WriteNumberValue(numberValue.Value);
                    }
                    else if (fv is DecimalValue decimalValue)
                    {
                        WriteDecimalValue(decimalValue.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Expected NumberValue (number) and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;
                case "boolean":
                    // bool
                    WritePropertyName(propertyName);

                    if (fv is BooleanValue booleanValue)
                    {
                        WriteBooleanValue(booleanValue.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Expected BooleanValue and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                case "integer":
                    // int16, int32, int64                    
                    WritePropertyName(propertyName);

                    if (fv is NumberValue integerValue)
                    {
                        WriteNumberValue(integerValue.Value);
                    }
                    else if (fv is DecimalValue decimalValue)
                    {
                        WriteDecimalValue(decimalValue.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Expected NumberValue (integer) and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                case "string":
                    // string, binary, date, date-time, password, byte (base64)
                    WritePropertyName(propertyName);

                    if (fv is StringValue stringValue)
                    {
                        WriteStringValue(stringValue.Value);
                    }
                    else if (fv is PrimitiveValue<DateTime> dt)
                    {
                        // DateTimeValue and DateValue
                        WriteDateTimeValue(dt.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Expected StringValue and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                case "object":
                    // collection of property/value pairs                                                         
                    WriteObject(propertyName, propertySchema, (fv as RecordValue).Fields);
                    break;

                default:
                    throw new NotImplementedException($"Not supported property type {propertySchema?.Type ?? "<null>"} for property {propertyName}");
            }
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
            else if (value is DecimalValue decimalValue)
            {
                WriteDecimalValue(decimalValue.Value);
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
