// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class DecimalLiteralNode : IntermediateNode
    {
        public readonly decimal LiteralValue;

        public DecimalLiteralNode(IRContext irContext, decimal value)
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
            return $"Decimal({LiteralValue})";
        }
    }
}
