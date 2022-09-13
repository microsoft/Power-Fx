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
    public class FormulaValueJsonConverter : JsonConverter<FormulaValue>
    {
        private readonly Dictionary<Type, Func<FormulaValue, PocoBase>> _customSerializers =
            new Dictionary<Type, Func<FormulaValue, PocoBase>>();

        private readonly Dictionary<string, Type> _customDeserializers = new Dictionary<string, Type>();

        private readonly Dictionary<string, Func<PocoBase, FormulaValue>> _customDeserializer2 =
            new Dictionary<string, Func<PocoBase, FormulaValue>>();

        public void Add<TFormulaValue, TPayload>(
            Func<TFormulaValue, TPayload> serialize,
            Func<TPayload, TFormulaValue> deserialize)
            where TFormulaValue : FormulaValue
        {
#pragma warning disable IDE0039 // Use local function
            var key = typeof(TFormulaValue).Name;

            Func<FormulaValue, PocoBase> s1 = (FormulaValue val) => new CustomRecordPoco<TPayload>
            {
                Kind = FormulaTypeSchema.ParamType.Record,
                CustomType = key,
                Payload = serialize((TFormulaValue)val)
            };

            Func<PocoBase, FormulaValue> s2 = (PocoBase poco) =>
                deserialize(((CustomRecordPoco<TPayload>)poco).Payload);

            _customDeserializer2.Add(key, s2);
            _customDeserializers.Add(key, typeof(CustomRecordPoco<TPayload>));

            _customSerializers.Add(typeof(TFormulaValue), s1);
        }

        private bool TryGetCustomPoco(FormulaValue value, out PocoBase poco)
        {
            if (_customSerializers.TryGetValue(value.GetType(), out var func))
            {
                poco = func(value);
                return true;
            }

            poco = null;
            return false;
        }

        public override void Write(Utf8JsonWriter writer, FormulaValue value, JsonSerializerOptions options)
        {
            // Primitives
            // Type
            // Value 
            PocoBase poco;
            if (value is NumberValue num)
            {
                poco = NumberPoco.New(num);
            }
            else if (value is RecordValue record)
            {
                // check for custom...
                if (!TryGetCustomPoco(value, out poco))
                {
                    poco = RecordPoco.New(record);
                }
            }
            else
            {
                throw new NotImplementedException($"Can't serialize {value.GetType().FullName}");
            }

            // Serialize as runtime type
            JsonSerializer.Serialize(writer, poco, poco.GetType(), options);
        }

        public override FormulaValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Tips for polymorphic deserialization 
            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-6-0#support-polymorphic-deserialization
            var r2 = reader;  //clone
            var r3 = r2;

            var kind = JsonSerializer.Deserialize<PocoKindBase>(ref r2, options);

            var type = kind.Kind switch
            {
                FormulaTypeSchema.ParamType.Number => typeof(NumberPoco),
                FormulaTypeSchema.ParamType.Record => typeof(RecordPoco),
                _ => throw new NotImplementedException($"{kind.Kind}")
            };

            var poco = (PocoBase)JsonSerializer.Deserialize(ref reader, type, options);

            if (poco is RecordPoco r && r.CustomType != null)
            {
                if (_customDeserializers.TryGetValue(r.CustomType, out var pocoType) &&
                    _customDeserializer2.TryGetValue(r.CustomType, out var f2))
                {
                    var poco2 = (PocoBase)JsonSerializer.Deserialize(ref r3, pocoType, options);
                    var val2 = f2(poco2);
                    return val2;
                }
            }

            var value = poco.ToValue();
            return value;
        }
    }
}
