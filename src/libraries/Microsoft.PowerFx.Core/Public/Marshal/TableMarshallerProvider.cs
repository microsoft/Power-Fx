// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Marshal .Net classes (with fields). This supports strong typing and lazy marshalling. 
    /// Handles any IEnumerable (including arrays).
    /// </summary>
    public class TableMarshallerProvider : ITypeMashallerProvider
    {
        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
        {
            if (!IsIEnumerableT(type, out var et))
            {
                marshaler = null;
                return false;
            }

            var tm = cache.GetMarshaller(et, maxDepth);
             
            if (tm.Type is RecordType recordType)
            {
                // Array of records 
            }       
            else
            {
                // Single Column table. Wrap the scalar in a record. 
                recordType = new RecordType().Add("Value", tm.Type);

                tm = new SCTMarshaler
                {
                    _inner = tm,
                    Type = recordType
                };
            }

            var tableType = recordType.ToTable();

            marshaler = new TableMarshaler 
            { 
                Type = tableType,
                _rowMarshaler = tm
            };
            return true;
        }

        // IEnumerable<T> --> T
        internal static bool IsIEnumerableT(Type collection, out Type elementType)
        {
            if (collection != typeof(string))
            {
                foreach (var t1 in collection.GetInterfaces())
                {
                    if (t1.IsGenericType && t1.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = t1.GenericTypeArguments[0];
                        return true;
                    }
                }
            }

            elementType = null;
            return false;
        }

        // Convert a single value into a Record for a SCT,  { value : x }
        internal class SCTMarshaler : ITypeMarshaller
        {
            public FormulaType Type { get; set; }

            public ITypeMarshaller _inner;

            public FormulaValue Marshal(object value)
            {
                var scalar = _inner.Marshal(value);
                var defaultField = new NamedValue("Value", scalar);                                

                var record = FormulaValue.NewRecordFromFields(defaultField);
                return record;
            }
        }

        internal class TableMarshaler : ITypeMarshaller
        {
            public FormulaType Type { get; set; }

            public ITypeMarshaller _rowMarshaler;

            public FormulaValue Marshal(object value)
            {
                var ir = IRContext.NotInSource(Type);

                return new LazyTableValue(ir, (IEnumerable)value, _rowMarshaler);
            }
        }
    }
        
    internal class LazyTableValue : TableValue
    {
        private readonly IEnumerable<DValue<RecordValue>> _rows;

        public override IEnumerable<DValue<RecordValue>> Rows => _rows;
                
        private readonly ITypeMarshaller _rowMarshaler;

        internal LazyTableValue(IRContext irContext, IEnumerable source, ITypeMarshaller rowMarshaler) 
            : base(irContext)
        {        
            _rowMarshaler = rowMarshaler;

            var rows = new List<DValue<RecordValue>>();
            foreach (var item in source)
            {
                var i2 = (RecordValue)_rowMarshaler.Marshal(item);
                rows.Add(DValue<RecordValue>.Of(i2));
            }

            _rows = rows;
        }
    }
}
