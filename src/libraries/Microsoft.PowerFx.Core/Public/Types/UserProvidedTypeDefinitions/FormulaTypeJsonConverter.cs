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
    internal class FormulaTypeJsonConverter : JsonConverter<FormulaType>
    {
        private readonly DefinedTypeSymbolTable _definedTypes;

        public FormulaTypeJsonConverter(DefinedTypeSymbolTable definedTypes)
        {
            _definedTypes = definedTypes;
        }

        public override FormulaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var schemaPoco = JsonSerializer.Deserialize<FormulaTypeSchema>(ref reader, options);
            return schemaPoco.ToFormulaType(_definedTypes);
        }

        public override void Write(Utf8JsonWriter writer, FormulaType value, JsonSerializerOptions options)
        {
            var schemaPoco = value.ToSchema(_definedTypes);
            JsonSerializer.Serialize(writer, schemaPoco, options);
        }
    }
}
