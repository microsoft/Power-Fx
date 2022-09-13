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
    // Schema files are a POCO object that can be serialized to Json. 
    // We translate between FormulaValues and Pocos. 
    internal class PocoKindBase
    {
        public FormulaTypeSchema.ParamType Kind { get; set; }
    }

    internal abstract class PocoBase : PocoKindBase
    {
        public abstract FormulaValue ToValue();
    }

    internal class NumberPoco : PocoBase
    {
        public double Value { get; set; }

        public static NumberPoco New(NumberValue val)
        {
            return new NumberPoco
            {
                Kind = FormulaTypeSchema.ParamType.Number,
                Value = val.Value
            };
        }

        public override FormulaValue ToValue()
        {
            return FormulaValue.New(Value);
        }
    }

    internal class RecordPoco : PocoBase
    {
        public string CustomType { get; set; }

        public FormulaType Type { get; set; }

        public Dictionary<string, FormulaValue> Fields { get; set; }

        public static RecordPoco New(RecordValue record)
        {
            var poco = new RecordPoco
            {
                Kind = FormulaTypeSchema.ParamType.Record,
                Type = record.Type,
                Fields = new Dictionary<string, FormulaValue>()
            };

            foreach (var field in record.Fields)
            {
                poco.Fields[field.Name] = field.Value;
            }

            return poco;
        }

        public override FormulaValue ToValue()
        {
            var recordType = (RecordType)Type;
            var fields = Fields.Select(kv => new NamedValue(kv.Key, kv.Value));
            var record = FormulaValue.NewRecordFromFields(recordType, fields);
            return record;
        }
    }

    internal class CustomRecordPoco<T> : PocoBase
    {
        public string CustomType { get; set; }

        public T Payload { get; set; }

        public override FormulaValue ToValue()
        {
            throw new NotImplementedException();
        }
    }
}
