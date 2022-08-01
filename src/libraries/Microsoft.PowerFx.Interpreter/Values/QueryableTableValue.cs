// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class QueryableTableValue : TableValue
    {
        private Task<List<DValue<RecordValue>>> _taskRows;
        private readonly object _lock = new object();

        internal QueryableTableValue(IRContext irContext)
            : base(irContext)
        {
        }

        internal abstract TableValue Filter(LambdaFormulaValue lambda);

        public bool HasCachedRows
        {
            get
            {
                lock (_lock)
                {
                    return _taskRows != null;
                }
            }
        }

        protected abstract Task<List<DValue<RecordValue>>> GetRowsAsync();

        public sealed override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                // Not the best, but doesn't require major breaking changes to TableValue
                lock (_lock)
                {
                    if (_taskRows == null)
                    {
                        _taskRows = GetRowsAsync();
                    }
                }

                return _taskRows.GetAwaiter().GetResult();
            }
        }
    }
}
