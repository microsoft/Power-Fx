// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// In-memory table. Constructed over RecordValues. 
    /// DValue means items could be error or blank. 
    /// </summary>
    internal class InMemoryTableValue : CollectionTableValue<DValue<RecordValue>>
    {
        private readonly RecordType _recordType;

        internal InMemoryTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
            : base(irContext, MaybeAdjustType(irContext, records).ToList())
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        // copy of rows made by constructor above with .ToList().
        internal InMemoryTableValue(InMemoryTableValue orig)
            : this(orig.IRContext, orig.Rows)
        {
        }

        public override bool TryShallowCopy(out FormulaValue copy)
        {
            copy = new InMemoryTableValue(this);
            return true;
        }

        public override bool CanShallowCopy => true;

        private static IEnumerable<DValue<RecordValue>> MaybeAdjustType(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
        {
            return records.Select(record => record.IsValue ? DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(((TableType)irContext.ResultType).ToRecord(), record.Value)) : record);
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> record)
        {
            return record;
        }

        protected override DValue<RecordValue> MarshalInverse(RecordValue row)
        {
            return DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(_recordType, row));
        }
    }

    // More constrained table when we know that all values are indeed Records, not error/blank. 
    // Beware of wrapping/unwrapping in DValues if we already have a RecordValue -
    // that can create extra IEnumerable wrappers that break direct indexing. 
    internal class RecordsOnlyTableValue : CollectionTableValue<RecordValue>
    {
        private readonly RecordType _recordType;

        internal RecordsOnlyTableValue(IRContext irContext, IEnumerable<RecordValue> records)
            : base(irContext, records)
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        internal RecordsOnlyTableValue(RecordsOnlyTableValue orig)
            : this(orig.IRContext, orig.Rows.Select(record => record.Value).ToList())
        {
        }

        public override bool TryShallowCopy(out FormulaValue copy)
        {
            copy = new RecordsOnlyTableValue(this);
            return true;
        }

        public override bool CanShallowCopy => true;

        protected override DValue<RecordValue> Marshal(RecordValue record)
        {
            return DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(_recordType, record));
        }

        protected override RecordValue MarshalInverse(RecordValue row)
        {
            return row;
        }
    }

    // An empty table.
    internal class EmptyTableValue : RecordsOnlyTableValue
    {
        internal EmptyTableValue(IRContext irContext)
            : base(irContext, System.Array.Empty<RecordValue>())
        {
        }

        public override bool TryShallowCopy(out FormulaValue copy)
        {
            // REVIEW: Shouldn't the instance be immutable? Can this safely just set copy = this?
            copy = new EmptyTableValue(this.IRContext);
            return true;
        }

        public override bool CanShallowCopy => true;
    }
}
