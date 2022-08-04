// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Types
{
    public abstract class QueryableTableValue : TableValue
    {
        private readonly Lazy<Task<List<DValue<RecordValue>>>> _lazyTaskRows;

        internal QueryableTableValue(IRContext irContext)
            : base(irContext)
        {
            _lazyTaskRows = new Lazy<Task<List<DValue<RecordValue>>>>(() => GetRowsAsync());
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
