// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// Context for the <see cref="ODataVisitor" />
    internal readonly struct ODataVisitorContext
    {
        internal readonly EvalVisitor Runner;
        internal readonly EvalVisitorContext Context;

        internal ODataVisitorContext(EvalVisitor runner, EvalVisitorContext context)
        {
            Runner = runner;
            Context = context;
        }

        internal ValueTask<FormulaValue> EvalAsync(IntermediateNode node) => node.Accept(Runner, Context);
    }
}
