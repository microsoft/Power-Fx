// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        internal const string TypeProperty = "Type";
        internal const string FieldsProperty = "Fields";

        internal const string Blank = "Blank";
        internal const string Boolean = "Boolean";
        internal const string Number = "Number";
        internal const string String = "String";
        internal const string Time = "Time";
        internal const string Date = "Date";
        internal const string DateTime = "DateTime";
        internal const string DateTimeNoTimeZone = "DateTimeNoTimezone";
        internal const string OptionSetValue = "OptionSetValue";
        internal const string Record = "Record";
        internal const string Table = "Table";

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // This is a one-way serializer, we don't expect to ever read types back from the client
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var typeName = value switch
            {
                BlankType => Blank,
                BooleanType => Boolean,
                NumberType => Number,
                StringType => String,
                TimeType => Time,
                DateType => Date,
                DateTimeType => DateTime,
                DateTimeNoTimeZoneType => DateTimeNoTimeZone,
                OptionSetValueType => OptionSetValue,
                TableType => Table,
                RecordType => Record,
                _ => throw new NotImplementedException($"Unknown {nameof(FormulaType)}: {value.GetType().Name}")
            };
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(TypeProperty) ?? TypeProperty);
            writer.WriteStringValue(typeName);

            if (typeName == Table)
            {
                WriteTableContents(writer, (TableType)value, options);
            }
            else if (typeName == Record)
            {
                WriteRecordContents(writer, (RecordType)value, options);
            }

            writer.WriteEndObject();
        }

        private void WriteRecordContents(Utf8JsonWriter writer, RecordType record, JsonSerializerOptions options)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(FieldsProperty) ?? FieldsProperty);
            writer.WriteStartObject();
            foreach (var namedFormulaType in record.GetNames())
            {
                writer.WritePropertyName(namedFormulaType.Name); // Type names ignore PropertyNamingPolicy
                if (namedFormulaType.Type == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    Write(writer, namedFormulaType.Type, options);
                }
            }

            writer.WriteEndObject();
        }

        private void WriteTableContents(Utf8JsonWriter writer, TableType table, JsonSerializerOptions options) =>
            WriteRecordContents(writer, table.ToRecord(), options);
    }
}
