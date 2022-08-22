// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Types
{
    internal readonly struct DelegationRunContext
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
