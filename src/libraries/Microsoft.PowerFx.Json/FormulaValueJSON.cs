﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx.Types
{
    public class FormulaValueJsonSerializerSettings
    {
        public bool NumberIsFloat { get; init; } = false;

        public bool ReturnUnknownRecordFieldsAsUntypedObjects { get; init; } = false;

        // JSON value input of type object may contain fields with values that not present in the target schema.
        // This attribute controls if the result such conversion should be valid or an error.
        public bool AllowUnknownRecordFields { get; init; } = true;

        public TimeZoneInfo ResultTimeZone { get; init; } = TimeZoneInfo.Utc;
    }

    internal class FormulaValueJsonSerializerWorkingData
    {
        public string Path { get; set; }
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
                    Message = $"{je.GetType().Name} {je.Message}",
                    Span = new Syntax.Span(0, 0),
                    Kind = ErrorKind.InvalidJSON
                });
            }
            catch (PowerFxJsonException pfxje)
            {
                return new ErrorValue(IRContext.NotInSource(formulaType), new ExpressionError()
                {
                    Message = $"{pfxje.GetType().Name} {pfxje.Message}",
                    Span = new Syntax.Span(0, 0),
                    Kind = ErrorKind.InvalidArgument
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
            return FromJson(element, settings, new FormulaValueJsonSerializerWorkingData(), formulaType);  
        }

        // // caller verified element is non-null and is of type string 
        internal static FormulaValue ParseDate(JsonElement element, FormulaType targetType, Func<DateTime, FormulaValue> funcParse)
        {
            var strValue = element.GetString(); 
            if (string.IsNullOrWhiteSpace(strValue))
            {
                return FormulaValue.NewBlank(targetType);
            }

            // Any exceptions will be caught at higher level. 
            var dateTime = element.GetDateTime();

            var value = funcParse(dateTime);
            return value;
        }

        internal static FormulaValue FromJson(JsonElement element, FormulaValueJsonSerializerSettings settings, FormulaValueJsonSerializerWorkingData data, FormulaType formulaType = null)
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
                        throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a Number", data.Path);
                    }

                case JsonValueKind.String:
                    if (skipTypeValidation || formulaType is StringType)
                    {
                        return StringValue.New(element.GetString());
                    }
                    else if (formulaType is DateType)
                    {
                        return ParseDate(
                            element,
                            FormulaType.Date,
                            (dateTime) => FormulaValue.NewDateOnly(dateTime.Date));
                    }
                    else if (formulaType is DateTimeType)
                    {
                        return ParseDate(
                            element,
                            FormulaType.DateTime,
                            (dt2) => 
                            {
                                if (dt2.Kind == DateTimeKind.Local)
                                {
                                    dt2 = dt2.ToUniversalTime();
                                }

                                if (settings.ResultTimeZone == TimeZoneInfo.Utc && dt2.Kind == DateTimeKind.Unspecified)
                                {
                                    dt2 = new DateTime(dt2.Ticks, DateTimeKind.Utc);
                                }

                                return DateTimeValue.New(DateTimeValue.GetConvertedDateTimeValue(dt2, settings.ResultTimeZone));
                            });
                    }
                    else if (formulaType is DateTimeNoTimeZoneType)
                    {
                        return ParseDate(
                            element, 
                            FormulaType.DateTime,
                            dt3 => DateTimeValue.New(TimeZoneInfo.ConvertTimeToUtc(dt3)));
                    }
                    else if (formulaType is TimeType)
                    {
                        var timeString = element.GetString();
                        if (TimeSpan.TryParseExact(timeString, @"hh\:mm\:ss\.FFFFFFF", CultureInfo.InvariantCulture, TimeSpanStyles.None, out var res) ||
                            TimeSpan.TryParseExact(timeString, @"hh\:mm\:ss", CultureInfo.InvariantCulture, TimeSpanStyles.None, out res))
                        {
                            return TimeValue.New(res);
                        }

                        throw new PowerFxJsonException($"Time '{timeString}' could not be parsed", data.Path);
                    }
                    else if (formulaType is BlobType)
                    {
                        return FormulaValue.NewBlob(element.GetBytesFromBase64());
                    }
                    else if (formulaType is OptionSetValueType osvt)
                    {
                        string el = element.GetString();

                        if (osvt.TryGetValue(new DName(el), out OptionSetValue osv))
                        {
                            return osv;
                        }

                        // If the OptionSetValue isn't found, let's just return an error
                        return new ErrorValue(IRContext.NotInSource(osvt), new ExpressionError()
                        {
                            Kind = ErrorKind.Validation,
                            Message = $"Invalid OptionSet value '{el}'"
                        });                        
                    }
                    else 
                    {
                        throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a String", data.Path);
                    }

                case JsonValueKind.False:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return BooleanValue.New(false);
                    }
                    else
                    {
                        throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a Boolean (false)", data.Path);
                    }

                case JsonValueKind.True:
                    if (skipTypeValidation || formulaType is BooleanType)
                    {
                        return BooleanValue.New(true);
                    }
                    else
                    {
                        throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a Boolean (true)", data.Path);
                    }

                case JsonValueKind.Object:
                    if (skipTypeValidation || formulaType is RecordType)
                    {
                        return RecordFromJsonObject(element, formulaType as RecordType, settings, data);
                    }
                    else if (formulaType is TableType tt)
                    {
                        // We should have received an array but as a best effort we'll read the record and return a 1-element table
                        RecordType rt = tt.ToRecord();
                        RecordValue rv = RecordFromJsonObject(element, rt, settings, data);
                        return TableValue.NewTable(rt, rv);
                    }
                    else
                    {
                        throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a Record", data.Path);
                    }

                case JsonValueKind.Array:
                    if (skipTypeValidation || formulaType is TableType)
                    {
                        return TableFromJsonArray(element, formulaType as TableType, settings, data);
                    }
                    else
                    {
                        int n = element.EnumerateArray().Count();

                        // If we receive a 1-element table, and that element is of the right type, let's accept it
                        if (n == 1)
                        {
                            JsonElement first = element.EnumerateArray().First();
                            return FromJson(first, settings, data, formulaType);
                        }
                        else
                        {
                            throw new PowerFxJsonException($"Expecting {formulaType._type.Kind} but received a Table with {n} elements", data.Path);
                        }
                    }

                default:
                    throw new PowerFxJsonException($"Unrecognized JsonElement {element.ValueKind}", data.Path);
            }
        }

        // Json objects parse to records.
        private static InMemoryRecordValue RecordFromJsonObject(JsonElement element, RecordType recordType, FormulaValueJsonSerializerSettings settings, FormulaValueJsonSerializerWorkingData data)
        {
            Contract.Assert(element.ValueKind == JsonValueKind.Object);

            var fields = new List<NamedValue>();
            var type = RecordType.Empty();

            foreach (var pair in element.EnumerateObject())
            {
                var name = pair.Name;
                var value = pair.Value;
                FormulaType fieldType = null;                

                if (recordType?.TryGetUnderlyingFieldType(name, out fieldType) == false)
                {
                    if (!settings.AllowUnknownRecordFields)
                    {
                        throw new PowerFxJsonException($"Unexpected field '{name}' found in JSONObject", $"{data.Path}/{name}");
                    }

                    // if we expect a record type and the field is unknown, let's ignore it like in Power Apps
                    if (!settings.ReturnUnknownRecordFieldsAsUntypedObjects)
                    {
                        continue;
                    }

                    // otherwise return the field as an Untyped Object (as it's not described in the swagger file)
                    fieldType = FormulaType.UntypedObject;
                }

                string newPath = string.IsNullOrEmpty(data.Path) ? name : $"{data.Path}/{name}";
                var paValue = FromJson(value, settings, new FormulaValueJsonSerializerWorkingData() { Path = newPath }, fieldType);
                fields.Add(new NamedValue(name, paValue));
                type = type.Add(new NamedFormulaType(name, paValue.IRContext.ResultType));
            }

            return new InMemoryRecordValue(IRContext.NotInSource(type), fields);
        }

        // More type safe than base class's ParseJson
        // Parse json.
        // [1,2,3]  is a single column table, actually equivalent to:
        // [{Value : 1, Value: 2, Value :3 }]
        internal static FormulaValue TableFromJsonArray(JsonElement array, TableType tableType, FormulaValueJsonSerializerSettings settings, FormulaValueJsonSerializerWorkingData data)
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
                var val = GuaranteeRecord(FromJson(element, settings, data, ft));

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
