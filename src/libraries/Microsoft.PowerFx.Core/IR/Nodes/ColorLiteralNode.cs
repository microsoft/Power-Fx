// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ColorLiteralNode : IntermediateNode
    {
        public readonly uint LiteralValue;

        public ColorLiteralNode(IRContext irContext, uint value)
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
            return $"Color({LiteralValue})";
        }
    }
}
