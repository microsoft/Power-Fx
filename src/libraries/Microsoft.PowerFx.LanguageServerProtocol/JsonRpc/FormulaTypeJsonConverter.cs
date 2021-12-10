
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private const string TypeProperty = "Type",
                             NamesProperty = "Names";

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
            var typeValue = GetPropertyCasingAware(element, TypeProperty, options).GetString();
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
                Record => RecordFromJson(element, options),
                Table => TableFromJson(element, options),
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
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(NamesProperty) ?? NamesProperty);
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

        private static FormulaType FromJson(JsonElement element, JsonSerializerOptions options)
        {
            var typeValue = GetPropertyCasingAware(element, TypeProperty, options).GetString();
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
                Record => RecordFromJson(element, options),
                Table => TableFromJson(element, options),
                _ => throw new NotImplementedException($"Unknown {nameof(FormulaType)}: {typeValue}")
            };
        }

        private static RecordType RecordFromJson(JsonElement element, JsonSerializerOptions options)
        {
            var record = new RecordType();

            if (!TryGetPropertyCasingAware(element, NamesProperty, options, out var names))
            {
                return record;
            }

            foreach (var pair in names.EnumerateObject())
            {
                record = record.Add(pair.Name, FromJson(pair.Value, options));
            }

            return record;
        }

        private static TableType TableFromJson(JsonElement element, JsonSerializerOptions options)
        {
            return RecordFromJson(element, options).ToTable();
        }

        private static JsonElement GetPropertyCasingAware(JsonElement element, string property, JsonSerializerOptions options)
        {
            if (TryGetPropertyCasingAware(element, property, options, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"The given property was not present in the JsonElement");
        }

        private static bool TryGetPropertyCasingAware(JsonElement element, string property, JsonSerializerOptions options, out JsonElement value)
        {
            if (!options.PropertyNameCaseInsensitive)
            {
                return element.TryGetProperty(property, out value);
            }

            if (element.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(property), out value))
            {
                return true;
            }
            return element.TryGetProperty(property, out value);
        }

    }
}
