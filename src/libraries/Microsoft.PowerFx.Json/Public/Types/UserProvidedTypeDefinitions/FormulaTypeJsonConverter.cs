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
    /// <summary>
    /// Converts FormulaType objects to and from JSON using System.Text.Json.
    /// </summary>
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
        /// Initializes a new instance of the <see cref="FormulaTypeJsonConverter"/> class with the specified serializer settings.
        /// </summary>
        /// <param name="settings">The serializer settings to use for formula type conversion.</param>
        public FormulaTypeJsonConverter(FormulaTypeSerializerSettings settings)
            : this(new SymbolTable())
        {
            _settings = settings ?? _settings;
        }

        /// <summary>
        /// Reads and converts the JSON to a <see cref="FormulaType"/> object.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options to use.</param>
        /// <returns>The deserialized <see cref="FormulaType"/> object.</returns>
        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, options);
            return schemaPoco.ToFormulaType(_definedTypes, _settings);
        }

        /// <summary>
        /// Writes a <see cref="FormulaType"/> object as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The <see cref="FormulaType"/> value to write.</param>
        /// <param name="options">The serializer options to use.</param>
        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = value.ToSchema(_definedTypes, _settings);
            JsonSerializer.Serialize(writer, schemaPoco, options);
        }
    }
}
