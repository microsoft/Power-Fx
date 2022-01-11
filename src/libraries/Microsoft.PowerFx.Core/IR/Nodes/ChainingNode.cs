// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ChainingNode : IntermediateNode
    {
        public readonly IList<IntermediateNode> Nodes;

        public ChainingNode(IRContext irContext, IList<IntermediateNode> nodes)
            : base(irContext)
        {
            Contracts.AssertAllValues(nodes);

            Nodes = nodes;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"Chained({string.Join(",", Nodes)})";
        }
    }
}
