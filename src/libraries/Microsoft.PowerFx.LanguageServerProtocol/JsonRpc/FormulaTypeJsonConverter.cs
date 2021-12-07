
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private const string TypeProperty = "$type";
        private const string Blank = "Blank",
                             Boolean = "Boolean",
                             Number = "Number",
                             String = "String",
                             Time = "Time",
                             Date = "Date",
                             DateTime = "DateTime",
                             DateTimeNoTimeZone = "DateTimeNoTimezone",
                             OptionSetValue = "OptionSetValue",
                             Record = "Record",
                             Table = "Table";

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonDocument.ParseValue(ref reader).RootElement;
            var typeValue = element.GetProperty(TypeProperty).GetString();
            return typeValue switch
            {
                Blank => FormulaType.Blank,
                Boolean => FormulaType.Boolean,
                Number => FormulaType.Number,
                String => FormulaType.String,
                Time => FormulaType.Time,
                Date => FormulaType.Date,
                DateTime => FormulaType.DateTime,
                DateTimeNoTimeZone => FormulaType.DateTimeNoTimeZone,
                OptionSetValue => FormulaType.DateTimeNoTimeZone,
                Record => RecordFromJson(element),
                Table => TableFromJson(element),
                _ => throw new NotImplementedException($"Unknown {nameof(FormulaType)}: {typeValue}")
            };
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
                OptionSetValueType => DateTimeNoTimeZone,
                TableType => Table,
                RecordType => Record,
                _ => throw new NotImplementedException($"Unknown {nameof(FormulaType)}: {value.GetType().Name}")
            };
            writer.WritePropertyName(TypeProperty);
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
            foreach (var namedFormulaType in record.GetNames())
            {
                writer.WritePropertyName(namedFormulaType.Name);
                if (namedFormulaType.Type == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    Write(writer, namedFormulaType.Type, options);
                }
            }
        }

        private void WriteTableContents(Utf8JsonWriter writer, TableType table, JsonSerializerOptions options) =>
            WriteRecordContents(writer, table.ToRecord(), options);

        private static FormulaType FromJson(JsonElement element)
        {
            var typeValue = element.GetProperty(TypeProperty).GetString();
            return typeValue switch
            {
                Blank => FormulaType.Blank,
                Boolean => FormulaType.Boolean,
                Number => FormulaType.Number,
                String => FormulaType.String,
                Time => FormulaType.Time,
                Date => FormulaType.Date,
                DateTime => FormulaType.DateTime,
                DateTimeNoTimeZone => FormulaType.DateTimeNoTimeZone,
                OptionSetValue => FormulaType.DateTimeNoTimeZone,
                Record => RecordFromJson(element),
                Table => TableFromJson(element),
                _ => throw new NotImplementedException($"Unknown {nameof(FormulaType)}: {typeValue}")
            };
        }

        private static RecordType RecordFromJson(JsonElement element)
        {
            var record = new RecordType();

            foreach (var pair in element.EnumerateObject())
            {
                if (pair.Name == TypeProperty)
                {
                    continue;
                }
                record = record.Add(pair.Name, FromJson(pair.Value));
            }

            return record;
        }

        private static TableType TableFromJson(JsonElement element)
        {
            return RecordFromJson(element).ToTable();
        }
    }
}
