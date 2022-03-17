// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Create a TableValue over a dotnet collection class.
    /// Depending on what collection interfaces it exposes will determine the capabilities 
    /// (such as Enumeration, Count, Index).
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    internal class ObjectCollectionTableValue<T> : TableValue
    {
        // Required - basic enumeration. This is the source object. 
        private readonly IEnumerable<T> _enumerator; // required. supports enumeration;

        // Additional capabilities. 
        private readonly IReadOnlyList<T> _sourceIndex; // maybe null. supports index. 
        private readonly IReadOnlyCollection<T> _sourceCount; // maybe null. supports count;

        // Convert T --> RecordValue
        private readonly ITypeMarshaller _rowMarshaler;

        /// <summary>
        /// This table can be enumerated. 
        /// </summary>
        public bool CanEnumerate => _enumerator != null;

        /// <summary>
        /// This table can be directly indexed (without a linear scan).
        /// </summary>
        public bool CanIndex => _sourceIndex != null;

        /// <summary>
        /// This table can return a count (without a linear scan). 
        /// </summary>
        public bool CanCount => _sourceCount != null;

        // Create a new TableValue that wraps the source. 
        // Operations are deferred lazily. 
        // The marshaller converts from the T to a RecordValue. 
        public static ObjectCollectionTableValue<T> New(IRContext irContext, IEnumerable<T> source, ITypeMarshaller rowMarshaler)
        {
            // Make this a static builder instead of a ctor so that we could return different derived classes 
            // based on the capabilities. 
            return new ObjectCollectionTableValue<T>(irContext, source, rowMarshaler);
        }

        internal ObjectCollectionTableValue(IRContext irContext, IEnumerable<T> source, ITypeMarshaller rowMarshaler)
         : base(irContext)
        {
            _enumerator = source;

            _sourceIndex = source as IReadOnlyList<T>;
            _sourceCount = source as IReadOnlyCollection<T>;

            _rowMarshaler = rowMarshaler;
        }

        protected virtual DValue<RecordValue> Marshal(T item)
        {
            var arg = _rowMarshaler.Marshal(item);
            var result = arg switch
            {
                RecordValue r => DValue<RecordValue>.Of(r),
                BlankValue b => DValue<RecordValue>.Of(b),
                _ => DValue<RecordValue>.Of((ErrorValue)arg),
            };
            return result;
        }

        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                foreach (var item in _enumerator)
                {
                    var record = Marshal(item);
                    yield return record;
                }
            }
        }

        public override int Count()
        {
            if (_sourceCount != null)
            {
                return _sourceCount.Count;
            }
            else
            {
                return base.Count();
            }
        }

        protected override bool TryGetIndex(int index0, out DValue<RecordValue> record)
        {
            if (_sourceIndex != null)
            {
                if (index0 < 0 || index0 >= _sourceCount.Count)
                {
                    record = null;
                    return false;
                }

                var item = _sourceIndex[index0];
                record = Marshal(item);
                return true;
            }
            else
            {
                return base.TryGetIndex(index0, out record);
            }
        }
    }
}
