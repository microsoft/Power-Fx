// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ResolvedObjectNode : IntermediateNode
    {
        public readonly object Value;

        public ResolvedObjectNode(IRContext irContext, object value) : base(irContext)
        {
            Contracts.AssertValue(value);

            Value = value;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"ResolvedObject({Value})";
        }
    }
}
