// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public abstract class QueryableTableValue : TableValue
    {
        private IAsyncEnumerable<DValue<RecordValue>> _cachedRows;

        public sealed override bool InMemory => false;

        internal QueryableTableValue(IRContext irContext)
            : base(irContext)
        {
            _cachedRows = null;
        }

        internal abstract TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue FirstN(int n);

        protected abstract IAsyncEnumerable<DValue<RecordValue>> GetRowsAsync();
        
        public sealed override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                _cachedRows ??= GetRowsAsync();
                ConfiguredCancelableAsyncEnumerable<DValue<RecordValue>>.Enumerator e = _cachedRows.ConfigureAwait(false).GetAsyncEnumerator();

                try
                {
                    while (e.MoveNextAsync().GetAwaiter().GetResult())
                    {
                        yield return e.Current;
                    }
                }
                finally
                {
                    e.DisposeAsync().GetAwaiter().GetResult();
                }
            }
        }

        protected void Refresh()
        {
            _cachedRows = null;
        }
    }
}
