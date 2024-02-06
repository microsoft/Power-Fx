// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    [ThreadSafeImmutable]
    internal abstract class FormulaValueSerializer
    {
        internal const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

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

        protected abstract void WriteDateTimeValueNoTimeZone(DateTime dateTimeValue);

        protected abstract void WriteDateValue(DateTime dateValue);

        protected readonly bool _schemaLessBody;
        protected readonly IConvertToUTC _utcConverter;

        internal FormulaValueSerializer(IConvertToUTC utcConverter, bool schemaLessBody)
        {
            _schemaLessBody = schemaLessBody;
            _utcConverter = utcConverter;
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

                if (namedValue == null || namedValue.Value.IsBlank())
                {
                    if (property.Value.IsInternal())
                    {
                        continue;
                    }

                    if (schema.Required.Contains(property.Key))
                    {
                        throw new PowerFxConnectorException($"Missing property {property.Key}, object is too complex or not supported");
                    }

                    continue;
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
                                DKind.Decimal => "number",
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
            if (fv is BlankValue || fv is ErrorValue)
            {
                return;
            }

            if (propertySchema == null)
            {
                throw new PowerFxConnectorException($"Missing schema for property {propertyName}");
            }

            // if connector has null as a type but "array" is provided, let's write it down. this is possible in case of x-ms-dynamic-properties
            if (fv is TableValue tableValue && ((propertySchema.Type ?? "array") == "array")) 
            {
                StartArray(propertyName);

                foreach (DValue<RecordValue> item in tableValue.Rows)
                {
                    StartArrayElement(propertyName);
                    RecordValue rva = item.Value;

                    // If we have an object schema, we will try to follow it
                    if (propertySchema.Items?.Type == "object" || propertySchema.Items?.Type == "array")
                    {
                        WriteProperty(null, propertySchema.Items, rva);
                        continue;
                    }

                    // Else, we write primitive types only
                    if (rva.Fields.Count() != 1)
                    {
                        throw new PowerFxConnectorException($"Incompatible Table for supporting array, RecordValue has more than one column - propertyName {propertyName}, number of fields {rva.Fields.Count()}");
                    }

                    if (rva.Fields.First().Name != "Value")
                    {
                        throw new PowerFxConnectorException($"Incompatible Table for supporting array, RecordValue doesn't have 'Value' column - propertyName {propertyName}");
                    }

                    WriteValue(rva.Fields.First().Value);
                }

                EndArray();
                return;
            }

            switch (propertySchema.Type)
            {
                case "null":
                    // nullable
                    throw new PowerFxConnectorException($"null schema type not supported yet for property {propertyName}");

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
                        throw new PowerFxConnectorException($"Expected NumberValue (number) and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
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
                        throw new PowerFxConnectorException($"Expected BooleanValue and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
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
                        throw new PowerFxConnectorException($"Expected NumberValue (integer) and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                case "string":
                    // string, binary, date, date-time, password, byte (base64)
                    WritePropertyName(propertyName);

                    if (fv is StringValue stringValue)
                    {
                        WriteStringValue(stringValue.Value);
                    }
                    else if (fv is DateTimeValue dtv)
                    {
                        if (propertySchema.Format == "date-time")
                        {
                            WriteDateTimeValue(_utcConverter.ToUTC(dtv));
                        }
                        else if (propertySchema.Format == "date-no-tz")
                        {
                            WriteDateTimeValueNoTimeZone(((PrimitiveValue<DateTime>)dtv).Value);
                        }
                        else
                        {
                            throw new PowerFxConnectorException($"Unknown {propertySchema.Format} format");
                        }
                    }
                    else if (fv is DateValue dv)
                    {
                        WriteDateValue(dv.GetConvertedValue(null));
                    }
                    else if (fv is BlobValue bv)
                    {
                        if (propertySchema.Format == "byte")
                        {
                            WriteStringValue(bv.GetAsBase64Async(CancellationToken.None).Result);
                        }
                        else
                        {
                            // "binary"
                            WriteStringValue(bv.GetAsStringAsync(null, CancellationToken.None).Result);
                        }
                    }
                    else
                    {
                        throw new PowerFxConnectorException($"Expected StringValue and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                // some connectors don't set "type" when they have dynamic schema
                // let's be tolerant and always write the passed record if the type was not provided
                case null:
                case "object":
                    if (fv is RecordValue recordValue)
                    {
                        WriteObject(propertyName, propertySchema, recordValue.Fields);
                    }
                    else
                    {
                        throw new PowerFxConnectorException($"Expected to get {propertySchema?.Type ?? "record"} for property {propertyName} but got {fv?.GetType().Name}");
                    }

                    break;

                default:
                    throw new PowerFxConnectorException($"Not supported property type {propertySchema?.Type ?? "<null>"} for property {propertyName}");
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
            else if (value is DateTimeValue dtv)
            {
                if (dtv.Type._type.Kind == DKind.DateTime)
                {
                    WriteDateTimeValue(dtv.GetConvertedValue(TimeZoneInfo.Local));
                }
                else if (dtv.Type._type.Kind == DKind.DateTimeNoTimeZone)
                {
                    WriteDateTimeValue(dtv.GetConvertedValue(TimeZoneInfo.Utc));
                }
                else
                {
                    throw new PowerFxConnectorException($"Unknown {dtv.Type._type.Kind} kind");
                }
            }
            else if (value is DateValue dv)
            {
                WriteDateValue(((PrimitiveValue<DateTime>)dv).Value);
            }
            else
            {
                throw new PowerFxConnectorException($"Not supported type {value.GetType().FullName} for value");
            }
        }
    }
}
