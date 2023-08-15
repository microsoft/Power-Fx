﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    // Class kept around for sequencing with formula bar changes,
    // remove after formula bar is supporting new json schema.
    [Obsolete("Use Microsoft.PowerFx.Core.FormulaTypeJsonConverter instead. This JSON representation of types is not supported.")]
    internal class FormulaTypeJsonConverter : JsonConverter<FormulaType>
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
                FormulaTypeSchema.ParamType.Decimal => FormulaType.Decimal,
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

                FormulaTypeSchema.ParamType.Unsupported => throw new NotImplementedException(),
                FormulaTypeSchema.ParamType.OptionSetValue => throw new NotImplementedException(),
                FormulaTypeSchema.ParamType.EntityRecord => throw new NotSupportedException(),
                FormulaTypeSchema.ParamType.EntityTable => throw new NotSupportedException(),
                _ => throw new NotImplementedException(),
            };
        }

        private FormulaType GetAggregateType(Dictionary<string, FormulaTypeSchema> fields, bool isTable)
        {
            Contracts.AssertValue(fields);
            Contracts.AssertAllValues(fields);

            var fieldsType = RecordType.Empty();
            foreach (var field in fields)
            {
                if (fieldsType.FieldNames.Contains(field.Key))
                {
                    throw new NotSupportedException($"Multiple definitions of {field.Key}");
                }

                fieldsType = fieldsType.Add(field.Key, GetFormulaType(field.Value));
            }

            return isTable ? fieldsType.ToTable() : fieldsType;
        }
    }
}
