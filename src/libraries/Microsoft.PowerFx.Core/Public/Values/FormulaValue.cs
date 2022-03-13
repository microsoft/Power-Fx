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
        // We place the .New*() methods on FormulaValue for discoverability. 
        // If we're "marshalling" a T, we need a TypeMarshallerCache
        // Else, if we're "constructing" a Table/Record from existing FormulaValues, we don't need a marshaller.
        // We can use C# overloading to resolve. 

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

        public static GuidValue New(Guid guid)
        {
            return new GuidValue(IRContext.NotInSource(FormulaType.Guid), guid);
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

        public static UntypedObjectValue New(IUntypedObject untypedObject)
        {
            return new UntypedObjectValue(
                IRContext.NotInSource(new UntypedObjectType()),
                untypedObject);
        }

        // Marshal an arbitray object (scalar, record, table, etc) into a FormulaValue. 
        public static FormulaValue New(object obj, Type type, TypeMarshallerCache cache = null)
        {
            if (cache == null)
            {
                cache = new TypeMarshallerCache();
            }

            // Have New() wrapper for discoverability. 
            return cache.Marshal(obj, type);   
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
        public static RecordValue NewRecord<T>(T obj, TypeMarshallerCache cache = null)
        {
            return NewRecord(obj, typeof(T), cache);
        }

        public static RecordValue NewRecord(object obj, Type type, TypeMarshallerCache cache = null)
        {
            if (cache == null)
            {
                cache = new TypeMarshallerCache();
            }

            var value = (RecordValue)cache.Marshal(obj, type);
            return value;
        }

        /// <summary>
        /// Create a record from the list of fields provided. 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static RecordValue NewRecordFromFields(params NamedValue[] fields)
        {
            return NewRecordFromFields(fields.AsEnumerable());
        }

        public static RecordValue NewRecordFromFields(IEnumerable<NamedValue> fields)
        {
            var type = new RecordType();
            foreach (var field in fields)
            {
                type = type.Add(new NamedFormulaType(field.Name, field.Value.IRContext.ResultType));
            }

            return NewRecordFromFields(type, fields);
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, params NamedValue[] fields)
        {
            return NewRecordFromFields(recordType, fields.AsEnumerable());
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, IEnumerable<NamedValue> fields)
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

        public static TableValue NewTable<T>(TypeMarshallerCache cache, params T[] array)
        {
            return NewTable(cache, (IEnumerable<T>)array);
        }

        // If T is a RecordValue or FormulaValue, this will call out to that overload. 
        public static TableValue NewTable<T>(TypeMarshallerCache cache, IEnumerable<T> rows)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            // For FormulaValue, we need to inspect the runtime type. 
            if (rows is IEnumerable<RecordValue> records)
            {
                return NewTable(records);
            }

            // Check FormulaValue after RecordValue
            if (rows is IEnumerable<FormulaValue> values)
            {
                // Check for a single-column table.                
                return NewSingleColumnTable(values);
            }

            // Statically marshal the T to a FormulaType.             
            var value = cache.Marshal(rows);
            return (TableValue)value;
        }
                
        // Already having RecordValues  (as oppossed to a unknown T) lets us avoid type marshalling.
        public static TableValue NewTable(params RecordValue[] values)
        {
            return NewTable((IEnumerable<RecordValue>)values);
        }

        public static TableValue NewTable(IEnumerable<RecordValue> records, TableType tableType = null)
        {
            if (tableType == null)
            {
                var first = records.FirstOrDefault();
                tableType = (first == null) ?
                    new TableType() :
                    ((RecordType)first.Type).ToTable();
            }

            return new InMemoryTableValue(IRContext.NotInSource(tableType), records.Select(r => DValue<RecordValue>.Of(r)));            
        }

        public static TableValue NewSingleColumnTable(params FormulaValue[] values)
        {
            return NewSingleColumnTable((IEnumerable<FormulaValue>)values);
        }

        public static TableValue NewSingleColumnTable(IEnumerable<FormulaValue> values)
        {
            var records = values.Select(value => GuaranteeRecord(value));
            return NewTable(records);
        }

        // Convert a FormulaValue into a Record for a single column table if needed. 
        private static RecordValue GuaranteeRecord(FormulaValue rawVal)
        {
            if (rawVal is RecordValue record)
            {
                return record;
            }

            // Handle the single-column-table case. 
            var defaultField = new NamedValue("Value", rawVal);

            var val = NewRecordFromFields(defaultField);
            return val;
        }

        // More type safe than base class's ParseJSON
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
