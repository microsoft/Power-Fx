// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class QueryableTableValue : TableValue
    {
        private Lazy<Task<List<DValue<RecordValue>>>> _lazyTaskRows;

        private Lazy<Task<List<DValue<RecordValue>>>> NewLazyTaskRowsInstance => new Lazy<Task<List<DValue<RecordValue>>>>(() => GetRowsAsync());

        internal QueryableTableValue(IRContext irContext)
            : base(irContext)
        {
            _lazyTaskRows = NewLazyTaskRowsInstance;
        }

        internal abstract TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue FirstN(int n);

        // internal abstract TableValue SortByColumns(FormulaValue[]);

        public bool HasCachedRows => _lazyTaskRows.IsValueCreated;

        protected abstract Task<List<DValue<RecordValue>>> GetRowsAsync();

        // Not the best, but doesn't require major breaking changes to TableValue
        public sealed override IEnumerable<DValue<RecordValue>> Rows =>
            _lazyTaskRows.Value.GetAwaiter().GetResult();

        public void Refresh()
        {
            _lazyTaskRows = NewLazyTaskRowsInstance;
        }
    }
}
