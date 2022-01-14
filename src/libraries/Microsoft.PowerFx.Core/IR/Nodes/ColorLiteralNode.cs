// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"Color({LiteralValue})";
        }
    }
}
