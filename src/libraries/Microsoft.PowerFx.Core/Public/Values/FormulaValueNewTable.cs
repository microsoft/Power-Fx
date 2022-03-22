﻿// Copyright (c) Microsoft Corporation.
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
    // Marshalling a tables. 
    // Tables need to know their record type.
    // - a ITypeMarshaller  (which can be obtained from a TypeMarshallerCache and a T)
    // - an explicit RecordType
    public partial class FormulaValue
    {
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
                var first = records.FirstOrDefault();
                var recordType = (first == null) ?
                    new RecordType() :
                    ((RecordType)first.Type);
                return NewTable(recordType, records);
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
        /// Construct a table from records. Assumed that Records must be the same type. 
        /// Already having RecordValues (as oppossed to a unknown T or errors) lets us avoid type marshalling.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static TableValue NewTable(RecordType recordType, params RecordValue[] values)
        {
            return NewTable(recordType, (IEnumerable<RecordValue>)values);
        }

        public static TableValue NewTable(RecordType recordType, IEnumerable<RecordValue> records)
        {
            var tableType = recordType.ToTable();
            return new RecordsOnlyTableValue(IRContext.NotInSource(tableType), records);
        }

        /// <summary>
        /// Convenience method to create a table over an array of primitives. 
        /// The Table will have 1 column 'Value'.
        /// </summary>
        /// <typeparam name="T">type of the primitive. This maps directly to a formula Value.</typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static TableValue NewSingleColumnTable<T>(params PrimitiveValue<T>[] values)
        {
            IEnumerable<PrimitiveValue<T>> list = values;
            return NewSingleColumnTable(list);
        }

        public static TableValue NewSingleColumnTable<T>(IEnumerable<PrimitiveValue<T>> values)
        {
            // This is a convenience method. For anything requiring more control,
            // use NewTable and be explicit. 
            if (!PrimitiveMarshallerProvider.TryGetFormulaType(typeof(T), out var fxType))
            {
                throw new InvalidOperationException($"Use NewTable() instead");
            }

            const string valueField = "Value";
            var recordType = new RecordType().Add(valueField, fxType);

            var irContext = IRContext.NotInSource(recordType);
            var recordValues = values.Select(item => new InMemoryRecordValue(
                irContext,
                new NamedValue[] { new NamedValue(valueField, item) }));

            return NewTable(recordType, recordValues);
        }
        #endregion
    }
}
