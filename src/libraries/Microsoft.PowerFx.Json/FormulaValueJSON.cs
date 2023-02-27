// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx.Types
{
    public class FormulaValueJSON
    {
        /// <summary>
        /// Convenience method to create a value from a json representation. 
        /// </summary>
        public static FormulaValue FromJson(string jsonString, FormulaType formulaType = null)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonString);
                using MemoryStream jsonMemStream = new MemoryStream();
                JsonElement propBag = document.RootElement;

                return FromJson(propBag, formulaType);
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
        /// <param name="formulaType">Expected formula type. We will check the Json element and formula type match if this parameter is provided.</param>
        public static FormulaValue FromJson(JsonElement element, FormulaType formulaType = null)
        {            
            if (formulaType is UntypedObjectType uot)
            {
                return new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), new JsonUntypedObject(element));
            }

            bool skipTypeValidation = formulaType == null || formulaType is BlankType;

            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));

                case JsonValueKind.Number:
                    if (skipTypeValidation || formulaType is NumberType)
                    {
                        return new NumberValue(IRContext.NotInSource(FormulaType.Number), element.GetDouble());
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a NumberType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.String:
                    if (skipTypeValidation || formulaType is StringType)
                    {
                        return new StringValue(IRContext.NotInSource(FormulaType.String), element.GetString());
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a StringType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.False:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a BooleanType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.True:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), true);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a BooleanType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.Object:
                    if (skipTypeValidation || formulaType is RecordType)
                    {
                        return RecordFromJsonObject(element, formulaType as RecordType);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a RecordType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.Array:
                    if (skipTypeValidation || formulaType is TableType)
                    {
                        return TableFromJsonArray(element, formulaType as TableType);
                    }                    
                    else
                    {
                        throw new NotImplementedException($"Expecting a TableType Json Array but got {formulaType._type.Kind}");
                    }

                default:
                    throw new NotImplementedException($"Unrecognized JsonElement {element.ValueKind}");
            }
        }

        // Json objects parse to records. 
        private static RecordValue RecordFromJsonObject(JsonElement element, RecordType recordType)
        {
            Contract.Assert(element.ValueKind == JsonValueKind.Object);

            var fields = new List<NamedValue>();
            var type = RecordType.Empty();

            foreach (var pair in element.EnumerateObject())
            {
                var name = pair.Name;
                var value = pair.Value;
                FormulaType fieldType = null;

                // if TryGetFieldType fails, fieldType is set to Blank
                if (recordType?.TryGetFieldType(name, out fieldType) != true)
                {
                    fieldType = null;
                }

                var paValue = FromJson(value, fieldType);
                fields.Add(new NamedValue(name, paValue));
                type = type.Add(new NamedFormulaType(name, paValue.IRContext.ResultType));
            }

            return new InMemoryRecordValue(IRContext.NotInSource(type), fields);
        }

        // More type safe than base class's ParseJson
        // Parse json. 
        // [1,2,3]  is a single column table, actually equivalent to: 
        // [{Value : 1, Value: 2, Value :3 }]
        internal static TableValue TableFromJsonArray(JsonElement array, TableType tableType)
        {
            Contract.Assert(array.ValueKind == JsonValueKind.Array);

            var records = new List<RecordValue>();
            var fields = tableType?.ToRecord().FieldNames;
            bool isArray = tableType?._type.IsColumn == true;
            FormulaType ft = isArray ? tableType.ToRecord().GetFieldType("Value") : tableType?.ToRecord();

            for (var i = 0; i < array.GetArrayLength(); ++i)
            {
                var element = array[i];
                var val = GuaranteeRecord(FromJson(element, ft));

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
