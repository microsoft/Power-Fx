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
    internal class InMemoryTableValue : CollectionTableValue<DValue<RecordValue>>, IMutationCopy
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

        FormulaValue IMutationCopy.ShallowCopy()
        {
            return new InMemoryTableValue(this);
        }

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
            return DValue<RecordValue>.Of(row);
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
