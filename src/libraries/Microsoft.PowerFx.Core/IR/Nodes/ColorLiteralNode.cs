// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Drawing;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class ColorLiteralNode : IntermediateNode
    {
        public readonly Color LiteralValue;

        public ColorLiteralNode(IRContext irContext, Color value)
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
