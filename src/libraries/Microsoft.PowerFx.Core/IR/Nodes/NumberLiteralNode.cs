// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class NumberLiteralNode : IntermediateNode
    {
        public readonly double LiteralValue;

        public NumberLiteralNode(IRContext irContext, double value) : base(irContext)
        {
            LiteralValue = value;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"Number({LiteralValue})";
        }
    }
}
