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
        private readonly SymbolTable _definedTypes;

        private readonly FormulaTypeSerializerSettings _settings;

        internal FormulaTypeJsonConverter(SymbolTable definedTypes)
        {
            _definedTypes = definedTypes;
            _settings = new FormulaTypeSerializerSettings(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeJsonConverter"/> class.
        /// </summary>
        public FormulaTypeJsonConverter()
            : this(settings: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaTypeJsonConverter"/> class.
        /// </summary>
        /// <param name="settings"></param>
        public FormulaTypeJsonConverter(FormulaTypeSerializerSettings settings)
            : this(new SymbolTable())
        {
            _settings = settings ?? _settings;
        }

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, options);
            return schemaPoco.ToFormulaType(_definedTypes, _settings);
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = value.ToSchema(_definedTypes, _settings);
            JsonSerializer.Serialize(writer, schemaPoco, options);
        }
    }
}
