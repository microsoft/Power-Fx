// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types.Serializer
{
    // DataverseRecordValue
    // Types? Especially for empty 
    // internal surface?

    // Serialization 
    public class FormulaValueSerializer
    {
        private readonly JsonSerializerOptions _options;

        private readonly FormulaValueJsonConverter _valueConverter = new FormulaValueJsonConverter();

        public FormulaValueSerializer()
        {
            _options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _options.Converters.Add(new JsonStringEnumConverter());
            _options.Converters.Add(_valueConverter);
            _options.Converters.Add(new FormulaTypeJsonConverter());
        }

        public string Serialize(FormulaValue value)
        {
            return JsonSerializer.Serialize(value, _options);
        }

        public FormulaValue Deserialize(string json)
        {
            var val = JsonSerializer.Deserialize<FormulaValue>(json, _options);
            return val;
        }

        // Custom for dervied FormulaValues. 
        // Provides the mapping to and from the Poco object. 
        public void Add<TFormulaType, TPayload>(
            Func<TFormulaType, TPayload> serialize,
            Func<TPayload, TFormulaType> deserialize)
            where TFormulaType : FormulaValue
        {
            _valueConverter.Add(serialize, deserialize);
        }
    }
}
