// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private readonly DefinedTypeSymbolTable _definedTypes;

        private readonly Func<string, RecordType> _logicalNameToRecordType;

        internal FormulaTypeJsonConverter(DefinedTypeSymbolTable definedTypes)
        {
            _definedTypes = definedTypes;
            _logicalNameToRecordType = (dummy) => throw new InvalidOperationException("Lazy type converter not registered");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeJsonConverter"/> class.
        /// </summary>
        /// <param name="logicalNameToRecordType">This is needed only for de-serialization for Dataverse types.</param>
        public FormulaTypeJsonConverter(Func<string, RecordType> logicalNameToRecordType)
        {
            _definedTypes = new DefinedTypeSymbolTable();
            Func<string, RecordType> debugHelper = (dummy) => throw new InvalidOperationException("Lazy type converter not registered");
            _logicalNameToRecordType = logicalNameToRecordType ?? debugHelper;
        }

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, options);
            return schemaPoco.ToFormulaType(_definedTypes, _logicalNameToRecordType);
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = value.ToSchema(_definedTypes, _logicalNameToRecordType);
            JsonSerializer.Serialize(writer, schemaPoco, options);
        }
    }
}
