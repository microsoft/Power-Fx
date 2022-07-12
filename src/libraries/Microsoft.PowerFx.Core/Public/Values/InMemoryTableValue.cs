// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// In-memory table. Constructed over RecordValues. 
    /// DValue means items could be error or blank. 
    /// </summary>
    internal class InMemoryTableValue : CollectionTableValue<DValue<RecordValue>>
    {
        private readonly BaseRecordType _recordType;

        internal InMemoryTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
            : base(irContext, records)
        {
            Contract.Assert(IRContext.ResultType is BaseTableType);
            var tableType = (BaseTableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();            
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> record)
        {
            if (record.IsValue)
            {
                var compileTimeType = _recordType;
                var record2 = CompileTimeTypeWrapperRecordValue.AdjustType(compileTimeType, record.Value);
                return DValue<RecordValue>.Of(record2);
            }
            else 
            {
                return record;
            }
        }
    }

    // More constrained table when we know that all values are indeed Records, not error/blank. 
    // Beware of wrapping/unwrapping in DValues if we already have a RecordValue -
    // that can create extra IEnumerable wrappers that break direct indexing. 
    internal class RecordsOnlyTableValue : CollectionTableValue<RecordValue>
    {
        private readonly BaseRecordType _recordType;

        internal RecordsOnlyTableValue(IRContext irContext, IEnumerable<RecordValue> records)
            : base(irContext, records)
        {
            Contract.Assert(IRContext.ResultType is BaseTableType);
            var tableType = (BaseTableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        protected override DValue<RecordValue> Marshal(RecordValue record)
        {
            return DValue<RecordValue>.Of(CompileTimeTypeWrapperRecordValue.AdjustType(_recordType, record));
        }
    }
}
