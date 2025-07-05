// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class NumberLiteralNode : IntermediateNode
    {
        public readonly double LiteralValue;
        public readonly UnitInfo UnitInfo;

        public NumberLiteralNode(IRContext irContext, double value, UnitInfo unitInfo = null)
            : base(irContext)
        {
            LiteralValue = value;
            UnitInfo = unitInfo;
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
}
