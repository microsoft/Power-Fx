// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class NumberLiteralNode : IntermediateNode
    {
        public readonly double LiteralValue;

        public NumberLiteralNode(IRContext irContext, double value)
            : base(irContext)
        {
            LiteralValue = value;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context, StackMarker stackMarker)
        {
            return visitor.Visit(this, context, stackMarker);
        }

        public override string ToString()
        {
            return $"Number({LiteralValue})";
        }
    }
}
