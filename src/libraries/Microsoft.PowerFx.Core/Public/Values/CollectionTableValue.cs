﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly ICollection<T> _sourceList;

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
            _sourceList = source as ICollection<T>;

            if (_sourceList != null && _sourceList.IsReadOnly)
            {
                _sourceList = null;
            }
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

        public override async Task<DValue<BooleanValue>> RemoveAsync(IEnumerable<FormulaValue> recordsToRemove, bool all, CancellationToken cancel)
        {
            var ret = false;

            if (_sourceList == null)
            {
                return await base.RemoveAsync(recordsToRemove, all, cancel);
            }

            foreach (RecordValue recordToRemove in recordsToRemove)
            {
                var deleteList = new List<T>();

                foreach (var item in _enumerator)
                {
                    cancel.ThrowIfCancellationRequested();

                    var dRecord = Marshal(item);

                    if (Matches(dRecord.Value, recordToRemove))
                    {
                        deleteList.Add(item);

                        if (!all)
                        {
                            break;
                        }
                    }
                }

                foreach (var delete in deleteList)
                {
                    _sourceList.Remove(delete);
                    ret = true;
                }
            }

            return DValue<BooleanValue>.Of(New(ret));
        }

        protected override async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord)
        {
            var actual = Find(baseRecord);

            if (actual != null)
            {
                return await actual.UpdateFieldsAsync(changeRecord);
            }
            else
            {
                return DValue<RecordValue>.Of(FormulaValue.NewBlank(IRContext.ResultType));
            }
        }

        /// <summary>
        /// Execute a linear search for the matching record.
        /// </summary>
        /// <param name="baseRecord">RecordValue argument.</param>
        /// <returns>A record instance within the current table. This record can then be updated.</returns>
        /// <remarks>A derived class may override if there's a more efficient way to find the match than by linear scan.</remarks>
        protected virtual RecordValue Find(RecordValue baseRecord)
        {
            foreach (var current in Rows)
            {
                if (Matches(current.Value, baseRecord))
                {
                    return current.Value;
                }
            }

            return null;
        }

        protected static bool Matches(RecordValue currentRecord, RecordValue baseRecord)
        {
            var ret = true;

            foreach (var baseRecordField in baseRecord.Fields)
            {
                var currentFieldValue = currentRecord.GetField(baseRecordField.Value.Type, baseRecordField.Name);

                if (currentFieldValue is BlankValue && baseRecordField.Value is BlankValue)
                {
                    continue;
                }
                else if (currentFieldValue is BlankValue)
                {
                    ret = false;
                    break;
                }
                else if (currentFieldValue.Type._type.IsPrimitive && baseRecordField.Value.Type._type.IsPrimitive)
                {
                    var compare1 = currentFieldValue.ToObject();
                    var compare2 = baseRecordField.Value.ToObject();

                    if (!compare1.Equals(compare2))
                    {
                        ret = false;
                        break;
                    }
                }
                else if (baseRecordField.Value is RecordValue baseRecordValue && currentFieldValue is RecordValue currentRecordValue)
                {
                    ret = Matches(currentRecordValue, baseRecordValue);
                }
                else
                {
                    throw new NotSupportedException("Field value not supported.");
                }
            }

            return ret;
        }
    }
}
