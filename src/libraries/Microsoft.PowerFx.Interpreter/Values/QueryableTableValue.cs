// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

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

        internal abstract TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context);

        internal abstract TableValue FirstN(int n);

        // internal abstract TableValue SortByColumns(FormulaValue[]);

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

    public readonly struct DelegationRunContext
    {
        internal readonly EvalVisitor Runner;
        internal readonly EvalVisitorContext Context;

        internal DelegationRunContext(EvalVisitor runner, EvalVisitorContext context)
        {
            Runner = runner;
            Context = context;
        }

        internal ValueTask<FormulaValue> EvalAsync(IntermediateNode node) => node.Accept(Runner, Context);
    }
}
