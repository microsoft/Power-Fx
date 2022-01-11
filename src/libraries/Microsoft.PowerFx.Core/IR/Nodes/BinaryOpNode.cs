// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class BinaryOpNode : IntermediateNode
    {
        public readonly BinaryOpKind Op;
        public readonly IntermediateNode Left;
        public readonly IntermediateNode Right;

        public BinaryOpNode(IRContext irContext, BinaryOpKind op, IntermediateNode left, IntermediateNode right) : base(irContext)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);

            Op = op;
            Left = left;
            Right = right;
        }


        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"BinaryOp({Op}, {Left}, {Right})";
        }
    }
}
