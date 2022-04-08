// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private readonly JsonSerializerOptions _options;

        public FormulaTypeJsonConverter()
        {
            _options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _options.Converters.Add(new JsonStringEnumConverter());
        }

        // This roundtrip is for testing only. These schemas should only be for communicating with the client, not rehydrating FormulaTypes
        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, _options);
            return GetFormulaType(schemaPoco);
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = FormulaTypeToSchemaConverter.Convert(value);
            JsonSerializer.Serialize(writer, schemaPoco, _options);
        }

        private FormulaType GetFormulaType(FormulaTypeSchema schema)
        {
            return schema.Type switch
            {
                FormulaTypeSchema.ParamType.Number => FormulaType.Number,
                FormulaTypeSchema.ParamType.String => FormulaType.String,
                FormulaTypeSchema.ParamType.Boolean => FormulaType.Boolean,
                FormulaTypeSchema.ParamType.Date => FormulaType.Date,
                FormulaTypeSchema.ParamType.Time => FormulaType.Time,
                FormulaTypeSchema.ParamType.DateTime => FormulaType.DateTime,
                FormulaTypeSchema.ParamType.Color => FormulaType.Color,
                FormulaTypeSchema.ParamType.Guid => FormulaType.Guid,                
                FormulaTypeSchema.ParamType.DateTimeNoTimeZone => FormulaType.DateTimeNoTimeZone,
                FormulaTypeSchema.ParamType.Blank => FormulaType.Blank,
                FormulaTypeSchema.ParamType.Hyperlink => FormulaType.Hyperlink,
                FormulaTypeSchema.ParamType.UntypedObject => FormulaType.UntypedObject,
                FormulaTypeSchema.ParamType.Record => GetAggregateType(schema.Fields, isTable: false),
                FormulaTypeSchema.ParamType.Table => GetAggregateType(schema.Fields, isTable: true),                
                
                FormulaTypeSchema.ParamType.OptionSetValue => throw new NotImplementedException(),
                FormulaTypeSchema.ParamType.EntityRecord => throw new NotSupportedException(),
                FormulaTypeSchema.ParamType.EntityTable => throw new NotSupportedException(),
            };
        }

        private FormulaType GetAggregateType(Dictionary<string, FormulaTypeSchema> fields, bool isTable)
        {
            Contracts.AssertValue(fields);
            Contracts.AssertAllValues(fields);

            var fieldsType = new RecordType();
            foreach (var field in fields)
            {
                if (fieldsType.MaybeGetFieldType(field.Key) != null)
                {
                    throw new NotSupportedException($"Multiple definitions of {field.Key}");
                }

                fieldsType = fieldsType.Add(field.Key, GetFormulaType(field.Value));
            }

            return isTable ? TableType.FromRecord(fieldsType) : fieldsType;
        }
    }
}
