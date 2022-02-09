// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represent a value in the formula expression. 
    /// </summary>
    [DebuggerDisplay("{ToObject().ToString()} ({Type})")]
    public abstract class FormulaValue
    {
        // IR contextual information flows from Binding >> IR >> Values
        // In general the interpreter should trust that the binding had
        // the correct runtime types for all values.
        internal IRContext IRContext { get; }

        public FormulaType Type => IRContext.ResultType;

        internal FormulaValue(IRContext irContext)
        {
            IRContext = irContext;
        }

        #region Host Utility API

        // Host utility creation methods, listed here for discoverability.
        // NOT FOR USE IN THE INTERPRETER! When creating new instances in
        // the interpreter, call the constructor directly and pass in the
        // IR context from the IR node.
        public static NumberValue New(double number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static FormulaValue New(double? number)
        {
            if (number.HasValue)
            {
                return New(number.Value);
            }

            return new BlankValue(IRContext.NotInSource(FormulaType.Number));
        }

        public static NumberValue New(decimal number)
        {
            // $$$ Is this safe? or loss in precision?
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), (double)number);
        }

        public static NumberValue New(long number)
        {
            // $$$ Is this safe? or loss in precision?
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), (double)number);
        }

        public static NumberValue New(int number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static NumberValue New(float number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static StringValue New(string value)
        {
            return new StringValue(IRContext.NotInSource(FormulaType.String), value);
        }

        public static BooleanValue New(bool value)
        {
            return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), value);
        }

        public static DateValue NewDateOnly(DateTime value)
        {
            if (value.TimeOfDay != TimeSpan.Zero)
            {
                throw new ArgumentException("Invalid DateValue, the provided DateTime contains a non-zero TimeOfDay");
            }

            if (value.Kind == DateTimeKind.Utc)
            {
                throw new ArgumentException("Invalid DateValue, the provided DateTime must be local");
            }

            return new DateValue(IRContext.NotInSource(FormulaType.Date), value);
        }

        public static DateTimeValue New(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                throw new ArgumentException("Invalid DateTimeValue, the provided DateTime must be local");
            }

            return new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), value);
        }

        public static TimeValue New(TimeSpan value)
        {
            return new TimeValue(IRContext.NotInSource(FormulaType.Time), value);
        }

        public static BlankValue NewBlank(FormulaType type = null)
        {
            if (type == null)
            {
                type = FormulaType.Blank;
            }

            return new BlankValue(IRContext.NotInSource(type));
        }

        public static ErrorValue NewError(ExpressionError error)
        {
            return new ErrorValue(IRContext.NotInSource(FormulaType.Blank), error);
        }

        public static TableValue NewTable<T>(params T[] array)
        {
            return NewTable((IEnumerable<T>)array);
        }

        public static TableValue NewTable<T>(IEnumerable<T> rows)
        {
            return TableFromEnumerable((System.Collections.IEnumerable)rows, typeof(T));
        }

        public static RecordValue NewRecord<T>(T obj)
        {
            return RecordFromProperties(obj, typeof(T));
        }

        public static RecordValue NewRecord(object obj, Type type)
        {
            return RecordFromProperties(obj, type);
        }

        public static UntypedObjectValue New(IUntypedObject untypedObject)
        {
            return new UntypedObjectValue(
                IRContext.NotInSource(new UntypedObjectType()),
                untypedObject);
        }

        // Dynamic new, useful for marshallers. 
        public static FormulaValue New(object obj, Type type)
        {
            if (obj == null)
            {
                return NewBlank();
            }

            if (obj is FormulaValue value)
            {
                return value;
            }

            if (obj is string strValue)
            {
                return New(strValue);
            }

            if (obj is bool boolValue)
            {
                return New(boolValue);
            }

            if (obj is double doubleValue)
            {
                return New(doubleValue);
            }

            if (obj is int intValue)
            {
                return New(intValue);
            }

            if (obj is decimal decValue)
            {
                return New(decValue);
            }

            if (obj is long longValue)
            {
                return New(longValue);
            }

            if (obj is float singleValue)
            {
                return New(singleValue);
            }

            if (obj is DateTime dateValue)
            {
                return New(dateValue);
            }

            if (obj is DateTimeOffset dateOffsetValue)
            {
                return New(dateOffsetValue.DateTime);
            }

            if (obj is TimeSpan timeValue)
            {
                return New(timeValue);
            }

            // Do checking off the static type, not the runtime instance. 
            if (type.IsInterface)
            {
                throw new InvalidOperationException($"Can't convert interface");
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return TableFromEnumerable((System.Collections.IEnumerable)obj, type.GetElementType());
            }

            // Record?
            return RecordFromProperties(obj, type);
        }

        /// <summary>
        /// Convenience method to create a value from a json representation. 
        /// </summary>
        /// <param name="jsonString"></param>
        public static FormulaValue FromJson(string jsonString)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonString);
                using var jsonMemStream = new MemoryStream();
                using var paJsonWriter = new Utf8JsonWriter(jsonMemStream);
                var propBag = document.RootElement;

                return FromJson(propBag);
            }
            catch
            {
                // $$$ better error handling here?
                throw;
            }
        }

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
        #endregion

        #region Host Records API

        /// <summary>
        /// Create a record by reflecting over the object's public properties.
        /// </summary>
        /// <typeparam name="T">static type to reflect over.</typeparam>
        /// <param name="obj"></param>
        /// <returns>a new record value.</returns>
        public static RecordValue RecordFromProperties<T>(T obj)
        {
            return RecordFromProperties(obj, typeof(T));
        }

        public static RecordValue RecordFromProperties(object obj, Type type)
        {
            var r = RecordFromFields(
                from prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                let fieldValue = prop.GetValue(obj)
                select new NamedValue(prop.Name, New(fieldValue, prop.PropertyType)));

            return r;
        }

        /// <summary>
        /// Create a record from the list of fields provided. 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static RecordValue RecordFromFields(params NamedValue[] fields)
        {
            return RecordFromFields(fields.AsEnumerable());
        }

        public static RecordValue RecordFromFields(IEnumerable<NamedValue> fields)
        {
            var type = new RecordType();
            foreach (var field in fields)
            {
                type = type.Add(new NamedFormulaType(field.Name, field.Value.IRContext.ResultType));
            }

            return new InMemoryRecordValue(IRContext.NotInSource(type), fields);
        }

        public static RecordValue RecordFromFields(RecordType recordType, params NamedValue[] fields)
        {
            return RecordFromFields(recordType, fields.AsEnumerable());
        }

        public static RecordValue RecordFromFields(RecordType recordType, IEnumerable<NamedValue> fields)
        {
            return new InMemoryRecordValue(IRContext.NotInSource(recordType), fields);
        }

        // Json objects parse to records. 
        internal static RecordValue RecordFromJsonObject(JsonElement element)
        {
            Contract.Assert(element.ValueKind == JsonValueKind.Object);

            var fields = new List<NamedValue>();
            var type = new RecordType();

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
        #endregion

        #region Host Tables API

        /// <summary>
        /// Create a table from an untyped IEnumerable. This can be useful in some dynamic scenarios.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        internal static TableValue TableFromEnumerable(
            System.Collections.IEnumerable values,
            Type elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            var records = TableToRecords(values, elementType);
            var recordType = (RecordType)records.First().IRContext.ResultType;
            return TableFromRecords(records, recordType.ToTable());
        }

        public static TableValue TableFromRecords(params RecordValue[] values)
        {
            var recordType = (RecordType)values.First().IRContext.ResultType;
            return TableFromRecords(values, recordType.ToTable());
        }

        public static TableValue TableFromRecords(IEnumerable<RecordValue> values, TableType type)
        {
            return new InMemoryTableValue(IRContext.NotInSource(type), values.Select(r => DValue<RecordValue>.Of(r)));
        }

        public static TableValue TableFromRecords<T>(IEnumerable<T> values, TableType type)
        {
            var values2 = values.Select(v => GuaranteeRecord(New(v, typeof(T))));
            return new InMemoryTableValue(IRContext.NotInSource(type), values2.Select(r => DValue<RecordValue>.Of(r)));
        }

        // Used for converting an arbitrary .net enumerable.
        // The types of the elements my be heterogenous even after this call
        private static IEnumerable<RecordValue> TableToRecords(
            System.Collections.IEnumerable values,
            Type elementType)
        {
            foreach (var obj in values)
            {
                var formulaValue = GuaranteeRecord(New(obj, elementType));
                yield return formulaValue;
            }
        }

        private static RecordValue GuaranteeRecord(FormulaValue rawVal)
        {
            if (rawVal is RecordValue record)
            {
                return record;
            }

            // Handle the single-column-table case. 
            var defaultField = new NamedValue("Value", rawVal);

            var val = RecordFromFields(defaultField);
            return val;
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
                type = new TableType();
            }
            else
            {
                type = TableType.FromRecord((RecordType)GuaranteeRecord(records[0]).IRContext.ResultType);
            }

            return new InMemoryTableValue(IRContext.NotInSource(type), records.Select(r => DValue<RecordValue>.Of(r)));
        }
        #endregion

        /// <summary>
        /// Converts to a .net object so host can easily consume the value. 
        /// Primitives (string, boolean, numbers, etc) convert directly to their .net type. 
        /// Records convert to a strongly typed or dynamic object so field notation works. 
        /// Tables convert to an enumerable of records. 
        /// </summary>
        /// <returns></returns>
        public abstract object ToObject();

        public abstract void Visit(IValueVisitor visitor);
    }
}
