// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal .Net classes (with fields). This supports strong typing and lazy marshalling. 
    /// Handles any IEnumerable (including arrays).
    /// </summary>
    public class TableMarshallerProvider : ITypeMarshallerProvider
    {
        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
        {
            if (!IsIEnumerableT(type, out var et))
            {
                marshaler = null;
                return false;
            }

            var rowMarshaller = cache.GetMarshaller(et, maxDepth);
             
            if (rowMarshaller.Type is not RecordType recordType)
            {               
                // Single Column table. Wrap in a record. 
                // This is happens for scalars.
                // But could also happen for a table of tables. 
                recordType = RecordType.Empty().Add(TableValue.ValueName, rowMarshaller.Type);

                rowMarshaller = new SCTMarshaller(recordType, rowMarshaller);
            }

            marshaler = TableMarshaller.Create(et, rowMarshaller);
            return true;
        }

        // IEnumerable<T> --> T
        internal static bool IsIEnumerableT(Type collection, out Type elementType)
        {
            if (collection != typeof(string))
            {
                if (Utility.TryGetElementType(collection, typeof(IEnumerable<>), out elementType))
                {
                    return true;
                }

                foreach (var t1 in collection.GetInterfaces())
                {
                    if (Utility.TryGetElementType(t1, typeof(IEnumerable<>), out elementType))
                    {                        
                        return true;
                    }
                }
            }

            elementType = null;
            return false;
        }

        // Convert a single value into a Record for a SCT,  { value : x }
        [DebuggerDisplay("SCT({_inner})")]
        internal class SCTMarshaller : ITypeMarshaller
        {
            public FormulaType Type { get; }

            private readonly ITypeMarshaller _inner;

            public SCTMarshaller(RecordType type, ITypeMarshaller inner)
            {
                Type = type;
                _inner = inner;
            }

            public FormulaValue Marshal(object value)
            {
                var scalar = _inner.Marshal(value);
                var defaultField = new NamedValue("Value", scalar);                                

                var record = FormulaValue.NewRecordFromFields(defaultField);
                return record;
            }
        }

        internal abstract class TableMarshaller : ITypeMarshaller
        {
            public FormulaType Type { get; private set; }

            protected ITypeMarshaller _rowMarshaller;

            // Create a TableMarshaller<T> where T is the given elementType.
            public static TableMarshaller Create(Type elementType, ITypeMarshaller rowMarshaller)
            {
                var t2 = typeof(TableMarshaller<>).MakeGenericType(elementType);
                var tableMarshaller = (TableMarshaller)Activator.CreateInstance(t2);

                var tableType = ((RecordType)rowMarshaller.Type).ToTable();

                tableMarshaller.Type = tableType;
                tableMarshaller._rowMarshaller = rowMarshaller;

                return tableMarshaller;
            }

            public abstract FormulaValue Marshal(object value);
        }

        // T is record type of the table. 
        [DebuggerDisplay("TableMarshal({_rowMarshaller})")]
        internal class TableMarshaller<T> : TableMarshaller
        {
            public override FormulaValue Marshal(object value)
            {
                var ir = IRContext.NotInSource(Type);

                var source = (IEnumerable<T>)value;
                return new ObjectCollectionTableValue<T>(ir, source, _rowMarshaller);
            }
        }
    }
}
