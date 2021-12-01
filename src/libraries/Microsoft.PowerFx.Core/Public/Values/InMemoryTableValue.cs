// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// In-memory table. 
    /// </summary>
    internal class InMemoryTableValue : TableValue
    {
        private readonly IEnumerable<DValue<RecordValue>> _records;

        public override IEnumerable<DValue<RecordValue>> Rows => _records;

        internal InMemoryTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> records) : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is TableType);
            var tableType = (TableType)IRContext.ResultType;
            var recordType = tableType.ToRecord();
            _records = records.Select(r =>
            {
                if (r.IsValue)
                {
                    return DValue<RecordValue>.Of(new InMemoryRecordValue(IRContext.NotInSource(recordType), r.Value.Fields));
                }
                else
                {
                    return r;
                }
            });
        }
    }
}
