// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx.Types
{
    public class FormulaValueJsonSerializerSettings
    {
        public bool NumberIsFloat { get; init; } = false;

        public bool ReturnUnknownRecordFieldsAsUntypedObjects { get; init; } = false;
    }

    public class FormulaValueJSON
    {
        /// <summary>
        /// Convenience method to create a value from a json representation. 
        /// </summary>
        public static FormulaValue FromJson(string jsonString, FormulaType formulaType = null, bool numberIsFloat = false)
        {
            return FromJson(jsonString, new FormulaValueJsonSerializerSettings() { NumberIsFloat = numberIsFloat }, formulaType);
        }
      
        public static FormulaValue FromJson(string jsonString, FormulaValueJsonSerializerSettings settings, FormulaType formulaType = null)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(jsonString);
                JsonElement propBag = document.RootElement;

                return FromJson(propBag, settings, formulaType);
            }
            catch (JsonException je)
            {                
                return new ErrorValue(IRContext.NotInSource(formulaType), new ExpressionError()
                {
                    Message = $"{je.GetType().Name} {je.Message} {je.StackTrace}",
                    Span = new Syntax.Span(0, 0),
                    Kind = ErrorKind.Network
                });
            }
        }

        /// <summary>
        /// Convenience method to create a value from a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="formulaType">Expected formula type. We will check the Json element and formula type match if this parameter is provided.</param>
        /// <param name="numberIsFloat">Treat JSON numbers as Floats.  By default, they are treated as Decimals.</param>         
        public static FormulaValue FromJson(JsonElement element, FormulaType formulaType = null, bool numberIsFloat = false)
        {
            return FromJson(element, new FormulaValueJsonSerializerSettings() { NumberIsFloat = numberIsFloat }, formulaType);
        }

        public static FormulaValue FromJson(JsonElement element, FormulaValueJsonSerializerSettings settings, FormulaType formulaType = null)
        {
            if (formulaType is UntypedObjectType uot)
            {
                return UntypedObjectValue.New(new JsonUntypedObject(element.Clone()));
            }

            bool skipTypeValidation = formulaType == null || formulaType is BlankType;

            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return FormulaValue.NewBlank(formulaType);

                case JsonValueKind.Number:
                    if ((skipTypeValidation && settings.NumberIsFloat) || formulaType is NumberType)
                    {
                        return NumberValue.New(element.GetDouble());
                    }
                    else if ((skipTypeValidation && !settings.NumberIsFloat) || formulaType is DecimalType)
                    {
                        return DecimalValue.New(element.GetDecimal());
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a NumberType or DecimalType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.String:
                    if (skipTypeValidation || formulaType is StringType)
                    {
                        return StringValue.New(element.GetString());
                    }
                    else if (formulaType is DateType)
                    {
                        DateTime dt1 = element.GetDateTime().Date;
                        return FormulaValue.NewDateOnly(dt1);
                    }
                    else if (formulaType is DateTimeType)
                    {
                        DateTime dt2 = element.GetDateTime();

                        if (dt2.Kind == DateTimeKind.Local)
                        {
                            dt2 = dt2.ToUniversalTime();
                        }

                        if (dt2.Kind == DateTimeKind.Unspecified)
                        {
                            dt2 = new DateTime(dt2.Ticks, DateTimeKind.Utc);
                        }

                        return DateTimeValue.New(dt2);
                    }
                    else if (formulaType is DateTimeNoTimeZoneType)
                    {
                        DateTime dt3 = element.GetDateTime();
                        return DateTimeValue.New(TimeZoneInfo.ConvertTimeToUtc(dt3));
                    }
                    else if (formulaType is BlobType)
                    {
                        return FormulaValue.NewBlob(element.GetBytesFromBase64());
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a StringType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.False:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return BooleanValue.New(false);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a BooleanType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.True:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return BooleanValue.New(true);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a BooleanType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.Object:
                    if (skipTypeValidation || formulaType is RecordType)
                    {
                        return RecordFromJsonObject(element, formulaType as RecordType, settings);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expecting a RecordType but got {formulaType._type.Kind}");
                    }

                case JsonValueKind.Array:
                    if (skipTypeValidation || formulaType is TableType)
                    {
                        return TableFromJsonArray(element, formulaType as TableType, settings);
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
        private static InMemoryRecordValue RecordFromJsonObject(JsonElement element, RecordType recordType, FormulaValueJsonSerializerSettings settings)
        {
            Contract.Assert(element.ValueKind == JsonValueKind.Object);

            var fields = new List<NamedValue>();
            var type = RecordType.Empty();

            foreach (var pair in element.EnumerateObject())
            {
                var name = pair.Name;
                var value = pair.Value;
                FormulaType fieldType = null;

                if (recordType?.TryGetFieldType(name, out fieldType) == false)
                {
                    // if we expect a record type and the field is unknown, let's ignore it like in Power Apps
                    if (!settings.ReturnUnknownRecordFieldsAsUntypedObjects)
                    {
                        continue;
                    }

                    // otherwise return the field as an Untyped Object (as it's not described in the swagger file)
                    fieldType = FormulaType.UntypedObject;
                }

                var paValue = FromJson(value, settings, fieldType);
                fields.Add(new NamedValue(name, paValue));
                type = type.Add(new NamedFormulaType(name, paValue.IRContext.ResultType));
            }

            return new InMemoryRecordValue(IRContext.NotInSource(type), fields);
        }

        // More type safe than base class's ParseJson
        // Parse json. 
        // [1,2,3]  is a single column table, actually equivalent to: 
        // [{Value : 1, Value: 2, Value :3 }]
        internal static FormulaValue TableFromJsonArray(JsonElement array, TableType tableType, FormulaValueJsonSerializerSettings settings)
        {
            Contract.Assert(array.ValueKind == JsonValueKind.Array);

            var records = new List<RecordValue>();

            // Single Column table (e.g. [1,2,3]) Pattern for table is unique
            // since in that case nested elements are not object and hence needs to be handled differently.
            var nestedElementsAreObjects = array.EnumerateArray().Any(nestedElement => nestedElement.ValueKind == JsonValueKind.Object);
            bool isArray = tableType?._type.IsColumn == true && !nestedElementsAreObjects;
            FormulaType ft;

            if (isArray)
            {
                if (array.GetArrayLength() == 0)
                {
                    ft = tableType?.ToRecord();
                }
                else
                {
                    ft = tableType?.ToRecord().GetFieldType("Value");
                }
            }
            else
            {
                ft = tableType?.ToRecord();
            }

            for (var i = 0; i < array.GetArrayLength(); ++i)
            {
                JsonElement element = array[i];
                var val = GuaranteeRecord(FromJson(element, settings, ft));

                records.Add(val);
            }

            // Constructor will handle both single-column table 
            TableType type;
            if (records.Count == 0)
            {
                // Keep expected table type when there is no record
                // so that the returned empty table has a matching type.
                type = tableType ?? TableType.Empty();
            }
            else
            {
                DType typeUnion = DType.EmptyRecord;
                foreach (var record in records)
                {
                    typeUnion = DType.Union(
                        GuaranteeRecord(record).IRContext.ResultType._type,
                        typeUnion,
                        useLegacyDateTimeAccepts: false,
                        features: Features.None /* Use more strict union rules */);
                }

                if (typeUnion.HasErrors)
                {
                    return new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), new JsonUntypedObject(array.Clone()));
                }

                type = (TableType)FormulaType.Build(typeUnion.ToRecord().ToTable());
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
