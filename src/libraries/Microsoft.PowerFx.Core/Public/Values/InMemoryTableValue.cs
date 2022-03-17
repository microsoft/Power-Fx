// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// In-memory table. Constructed over RecordValues. 
    /// DValue means items could be error or blank. 
    /// </summary>
    internal class InMemoryTableValue : ObjectCollectionTableValue<DValue<RecordValue>>
    {
        private readonly RecordType _recordType;

        internal InMemoryTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
            : base(irContext, records, null)
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();            
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> record)
        {
            if (record.IsValue)
            {
                return DValue<RecordValue>.Of(
                    new InMemoryRecordValue(IRContext.NotInSource(_recordType), record.Value.Fields));
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
    internal class RecordsOnlyTableValue : ObjectCollectionTableValue<RecordValue>
    {
        private readonly RecordType _recordType;

        internal RecordsOnlyTableValue(IRContext irContext, IEnumerable<RecordValue> records)
            : base(irContext, records, null)
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            _recordType = tableType.ToRecord();
        }

        protected override DValue<RecordValue> Marshal(RecordValue record)
        {
            return DValue<RecordValue>.Of(
                 new InMemoryRecordValue(IRContext.NotInSource(_recordType), record.Fields));
        }
    }
}
