// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class FormulaValueJSON
    {
        /// <summary>
        /// Convenience method to create a value from a json representation. 
        /// </summary>
        public static FormulaValue FromJson(string jsonString)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonString);
                using var jsonMemStream = new MemoryStream();
                var propBag = document.RootElement;

                return FromJson(propBag);
            }
            catch
            {
                // $$$ better error handling here?
                throw;
            }
        }

        /// <summary>
        /// Convenience method to create a value from a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="element"></param>
        public static FormulaValue FromJson(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));

                case JsonValueKind.Number:
                    return new NumberValue(IRContext.NotInSource(FormulaType.Number), element.GetDouble());

                case JsonValueKind.String:
                    return new StringValue(IRContext.NotInSource(FormulaType.String), element.GetString());

                case JsonValueKind.False:
                    return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);

                case JsonValueKind.True:
                    return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), true);

                case JsonValueKind.Object:
                    return RecordFromJsonObject(element);

                case JsonValueKind.Array:
                    return TableFromJsonArray(element);

                default:
                    throw new NotImplementedException($"Unrecognized JsonElement {element.ValueKind}");
            }
        }

        // Json objects parse to records. 
        private static RecordValue RecordFromJsonObject(JsonElement element)
        {
            Contract.Assert(element.ValueKind == JsonValueKind.Object);

            var fields = new List<NamedValue>();
            var type = RecordType.Empty();

            foreach (var pair in element.EnumerateObject())
            {
                var name = pair.Name;
                var value = pair.Value;

                var paValue = FromJson(value);
                fields.Add(new NamedValue(name, paValue));
                type = type.Add(new NamedFormulaType(name, paValue.IRContext.ResultType));
            }

            return new InMemoryRecordValue(IRContext.NotInSource(type), fields);
        }

        // More type safe than base class's ParseJson
        // Parse json. 
        // [1,2,3]  is a single column table, actually equivalent to: 
        // [{Value : 1, Value: 2, Value :3 }]
        internal static TableValue TableFromJsonArray(JsonElement array)
        {
            Contract.Assert(array.ValueKind == JsonValueKind.Array);

            var records = new List<RecordValue>();

            for (var i = 0; i < array.GetArrayLength(); ++i)
            {
                var element = array[i];
                var val = GuaranteeRecord(FromJson(element));

                records.Add(val);
            }

            // Constructor will handle both single-column table 
            TableType type;
            if (records.Count == 0)
            {
                type = TableType.Empty();
            }
            else
            {
                type = ((RecordType)GuaranteeRecord(records[0]).IRContext.ResultType).ToTable();
            }

            return new InMemoryTableValue(IRContext.NotInSource(type), records.Select(r => DValue<RecordValue>.Of(r)));
        }

        // Convert a FormulaValue into a Record for a single column table if needed. 
        internal static RecordValue GuaranteeRecord(FormulaValue rawVal)
        {
            if (rawVal is RecordValue record)
            {
                return record;
            }

            // Handle the single-column-table case. 
            var defaultField = new NamedValue(TableValue.ValueName, rawVal);

            var val = FormulaValue.NewRecordFromFields(defaultField);
            return val;
        }
    }
}
