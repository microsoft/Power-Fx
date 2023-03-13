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

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"{LiteralValue}:n";
        }
    }

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
            return $"{LiteralValue}:w";
        }
    }
}
