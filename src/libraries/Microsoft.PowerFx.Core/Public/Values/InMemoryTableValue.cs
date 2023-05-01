// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// In-memory table. Constructed over RecordValues. 
    /// DValue means items could be error or blank. 
    /// </summary>
    internal class InMemoryTableValue : CollectionTableValue<DValue<RecordValue>>
    {
        private readonly RecordType _recordType;

        public override bool IsMutable => true;

        internal InMemoryTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
            : base(irContext, MaybeAdjustType(irContext, records).ToList())
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        private static IEnumerable<DValue<RecordValue>> MaybeAdjustType(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
        {
            return records.Select(record => record.IsValue ? DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(((TableType)irContext.ResultType).ToRecord(), record.Value)) : record);
        }

        internal override IEnumerable<DValue<RecordValue>> ShallowCopyRows(IEnumerable<DValue<RecordValue>> records)
        {
            var copy = new List<DValue<RecordValue>>();

            foreach (var record in records)
            {
                copy.Add(record.IsValue ? DValue<RecordValue>.Of((RecordValue)record.Value.ShallowCopy()) : record);
            }

            return copy;
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> record)
        {
            return record;
        }

        protected override DValue<RecordValue> MarshalInverse(RecordValue row)
        {
            return DValue<RecordValue>.Of(row);
        }
    }

    // More constrained table when we know that all values are indeed Records, not error/blank. 
    // Beware of wrapping/unwrapping in DValues if we already have a RecordValue -
    // that can create extra IEnumerable wrappers that break direct indexing. 
    internal class RecordsOnlyTableValue : CollectionTableValue<RecordValue>
    {
        private readonly RecordType _recordType;

        public override bool IsMutable => false;

        internal RecordsOnlyTableValue(IRContext irContext, IEnumerable<RecordValue> records)
            : base(irContext, records)
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        internal override IEnumerable<RecordValue> ShallowCopyRows(IEnumerable<RecordValue> records)
        {
            return records.Select(item => (RecordValue)item.ShallowCopy());
        }

        protected override DValue<RecordValue> Marshal(RecordValue record)
        {
            return DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(_recordType, record));
        }

        protected override RecordValue MarshalInverse(RecordValue row)
        {
            return row;
        }
    }
}
