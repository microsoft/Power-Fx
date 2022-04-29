// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal .net objects into Power Fx values.  
    /// This allows customizing the marshallers, as well as caching the conversion rules for a given type. 
    /// This is an immutable object representing a collection of immutable providers. 
    /// </summary>
    public static class TypeMarshallerCacheExtensions
    {
        public static TableValue NewTable<T>(this TypeMarshallerCache cache, params T[] array)
        {
            return cache.NewTable((IEnumerable<T>)array);
        }

        // If T is a RecordValue or FormulaValue, this will call out to that overload. 
        public static TableValue NewTable<T>(this TypeMarshallerCache cache, IEnumerable<T> rows)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            // For FormulaValue, we need to inspect the runtime type. 
            if (rows is IEnumerable<RecordValue> records)
            {
                var first = records.FirstOrDefault();
                var recordType = (first == null) ?
                    new RecordType() :
                    ((RecordType)first.Type);
                return FormulaValue.NewTable(recordType, records);
            }

            if (rows is IEnumerable<FormulaValue>)
            {
                // Check to help avoid common errors. 
                throw new InvalidOperationException($"Use NewSingleColumnTable instead");
            }

            // Statically marshal the T to a FormulaType.             
            var value = cache.Marshal(rows);
            return (TableValue)value;
        }

        /// <summary>
        /// Create a record by reflecting over the object's public properties.
        /// </summary>
        /// <typeparam name="T">static type to reflect over.</typeparam>
        /// <param name="cache"></param>
        /// <param name="obj"></param>
        /// <returns>a new record value.</returns>
        public static RecordValue NewRecord<T>(this TypeMarshallerCache cache, T obj)
        {
            return cache.NewRecord(obj, typeof(T));
        }

        public static RecordValue NewRecord(this TypeMarshallerCache cache, object obj, Type type)
        {
            var value = (RecordValue)cache.Marshal(obj, type);
            return value;
        }
    }
}
