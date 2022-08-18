// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Create a TableValue over a dotnet collection class.
    /// Depending on what collection interfaces it exposes will determine the capabilities 
    /// (such as Enumeration, Count, Index).
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    public abstract class CollectionTableValue<T> : TableValue
    {
        // Required - basic enumeration. This is the source object. 
        private readonly IEnumerable<T> _enumerator; // required. supports enumeration;

        // Additional capabilities. 
        private readonly IReadOnlyList<T> _sourceIndex; // maybe null. supports index. 
        private readonly IReadOnlyCollection<T> _sourceCount; // maybe null. supports count;

        public CollectionTableValue(RecordType recordType, IEnumerable<T> source)
          : this(IRContext.NotInSource(recordType.ToTable()), source)
        {
            RecordType = recordType;
        }

        internal CollectionTableValue(IRContext irContext, IEnumerable<T> source)
         : base(irContext)
        {
            _enumerator = source ?? throw new ArgumentNullException(nameof(source));

            _sourceIndex = source as IReadOnlyList<T>;
            _sourceCount = source as IReadOnlyCollection<T>;
            _sourceList = source as List<T>;
        }

        public RecordType RecordType { get; }

        protected abstract DValue<RecordValue> Marshal(T item);

        protected virtual T MarshalInverse(RecordValue row)
        {
            throw new NotImplementedException();
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

        private readonly List<T> _sourceList;

        public override async Task<DValue<RecordValue>> AppendAsync(RecordValue record)
        {
            if (_sourceList == null)
            {
                return await base.AppendAsync(record);
            }

            var item = MarshalInverse(record);

            _sourceList.Add(item);

            return DValue<RecordValue>.Of(record);
        }

        protected override bool TryGetIndex(int index1, out DValue<RecordValue> record)
        {
            var index0 = index1 - 1;
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
                return base.TryGetIndex(index1, out record);
            }
        }
    }
}
