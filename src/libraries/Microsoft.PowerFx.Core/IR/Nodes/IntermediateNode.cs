// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal abstract class IntermediateNode
    {
        public IRContext IRContext { get; }

        public IntermediateNode(IRContext irContext)
        {
            IRContext = irContext;
        }

        public abstract TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context);
    }
}
