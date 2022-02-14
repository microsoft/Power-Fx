// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class StringInterpolationNode : IntermediateNode
    {
        public readonly IList<IntermediateNode> Nodes;

        public StringInterpolationNode(IRContext irContext, IList<IntermediateNode> nodes)
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
            return $"StringInterpolation({string.Join(",", Nodes)})";
        }
    }
}
