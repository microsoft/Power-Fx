// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

        // if true, need to override GetHttpClient
        // if false, need to override GetResult
        internal virtual bool GeneratesHttpContent { get; } = false;

        internal virtual string GetResult() => null;

        internal virtual HttpContent GetHttpContent() => null;

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

        protected abstract Task WriteBlobValueAsync(BlobValue blobValue, ISwaggerSchema schema);

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

        internal async Task SerializeValueAsync(string paramName, ISwaggerSchema schema, FormulaValue value)
        {
            await WritePropertyAsync(paramName, schema, value).ConfigureAwait(false);
        }

        private async Task WriteObjectAsync(string objectName, ISwaggerSchema schema, IEnumerable<NamedValue> fields)
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

                await WritePropertyAsync(property.Key, property.Value, namedValue.Value).ConfigureAwait(false);
            }

            if (!schema.Properties.Any() && fields.Any())
            {
                foreach (NamedValue nv in fields)
                {
                    await WritePropertyAsync(
                        nv.Name,
                        new SwaggerSchema(
                            type: GetType(nv.Value.Type),
                            format: GetFormat(nv.Value.Type)),
                        nv.Value).ConfigureAwait(false);
                }
            }

            EndObject(objectName);
        }

        internal static string GetType(FormulaType type)
        {
            return type._type.Kind switch
            {
                DKind.Number => "number",
                DKind.Decimal => "number",
                DKind.String or
                DKind.Date or
                DKind.DateTime or
                DKind.DateTimeNoTimeZone => "string",
                DKind.Boolean => "boolean",
                DKind.Record => "object",
                DKind.Table => "array",
                DKind.ObjNull => "null",
                _ => $"type: unknown_dkind {type._type.Kind}"
            };
        }

        internal static string GetFormat(FormulaType type)
        {
            return type._type.Kind switch
            {
                DKind.Date => "date",
                DKind.DateTime => "date-time",
                DKind.DateTimeNoTimeZone => "date-no-tz",
                _ => null
            };
        }

        private async Task WritePropertyAsync(string propertyName, ISwaggerSchema propertySchema, FormulaValue fv)
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

                // If we have an object schema, we will try to follow it
                if (propertySchema.Items?.Type == "object" || propertySchema.Items?.Type == "array")
                {
                    foreach (DValue<RecordValue> item in tableValue.Rows)
                    {
                        if (!item.IsError)
                        {
                            StartArrayElement(null);
                            RecordValue rva = item.Value;

                            await WritePropertyAsync(null, propertySchema.Items, rva).ConfigureAwait(false);
                        }
                    }
                }
                else if (tableValue.Rows.All(r => r.Value.Fields.Count() == 1 && r.Value.Fields.First().Name == "Value"))
                {
                    // Working with an array of simply types
                    foreach (DValue<RecordValue> item in tableValue.Rows)
                    {
                        if (!item.IsError)
                        {
                            StartArrayElement(null);
                            RecordValue rva = item.Value;
                            WriteValue(rva.Fields.First().Value);
                        }
                    }
                }
                else 
                {
                    // Working with untyped and unknown complex objects
                    foreach (DValue<RecordValue> item in tableValue.Rows)
                    {
                        if (!item.IsError)
                        {
                            // Add objects
                            StartArrayElement(null);
                            RecordValue rva = item.Value;
                            await WriteObjectAsync(null, new SwaggerSchema(type: GetType(rva.Type), format: GetFormat(rva.Type)), rva.Fields).ConfigureAwait(false);
                        }
                    }
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
                        await WriteBlobValueAsync(blobValue: bv, schema: propertySchema).ConfigureAwait(false);
                    }
                    else if (fv is OptionSetValue optionSetValue)
                    {
                        WriteStringValue(optionSetValue.Option);
                    }
                    else
                    {
                        throw new PowerFxConnectorException($"Expected StringValue and got {fv?.GetType()?.Name ?? "<null>"} value, for property {propertyName}");
                    }

                    break;

                case "file" when fv is BlobValue blobValue:
                    WritePropertyName(propertyName);
                    await WriteBlobValueAsync(blobValue: blobValue, schema: propertySchema).ConfigureAwait(false);
                    break;

                // some connectors don't set "type" when they have dynamic schema
                // let's be tolerant and always write the passed record if the type was not provided
                case null:
                case "object":
                    if (fv is RecordValue recordValue)
                    {
                        await WriteObjectAsync(propertyName, propertySchema, recordValue.Fields).ConfigureAwait(false);
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
