// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Marshalling a tables. 
    // Tables need to know their record type.
    // - a ITypeMarshaller  (which can be obtained from a TypeMarshallerCache and a T)
    // - an explicit RecordType
    public partial class FormulaValue
    {
        /// <summary>
        /// Construct a table from records. Assumed that Records must be the same type. 
        /// Already having RecordValues (as oppossed to a unknown T or errors) lets us avoid type marshalling.
        /// </summary>
        /// <param name="recordType"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static TableValue NewTable(RecordType recordType, params RecordValue[] values)
        {
            var list = new List<RecordValue>(values);
            return NewTable(recordType, list);
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
            if (!PrimitiveValueConversions.TryGetFormulaType(typeof(T), out var fxType))
            {
                throw new InvalidOperationException($"Use NewTable() instead");
            }

            var recordType = RecordType.Empty().Add(TableValue.ValueName, fxType);

            var irContext = IRContext.NotInSource(recordType);
            var recordValues = values.Select(item => new InMemoryRecordValue(
                irContext,
                new NamedValue[] { new NamedValue(TableValue.ValueName, item) }));

            return NewTable(recordType, recordValues);
        }
    }
}
