// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    /// <summary>
    /// Binary operation parse node. Example:
    /// 
    /// <code>Left BinaryOp Right</code>
    /// </summary>
    public sealed class BinaryOpNode : TexlNode
    {
        /// <summary>
        /// Left operand of the binary operation.
        /// </summary>
        public TexlNode Left { get; }

        /// <summary>
        /// Right operand of the binary operation.
        /// </summary>
        public TexlNode Right { get; }

        /// <summary>
        /// The binary operator.
        /// </summary>
        public BinaryOp Op { get; }

        internal BinaryOpNode(ref int idNext, Token primaryToken, SourceList sourceList, BinaryOp op, TexlNode left, TexlNode right)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);
            Left = left;
            Left.Parent = this;
            Right = right;
            Right.Parent = this;
            Op = op;
            _depth = 1 + (left.Depth > right.Depth ? left.Depth : right.Depth);

            MinChildID = Math.Min(left.MinChildID, right.MinChildID);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var left = Left.Clone(ref idNext, ts);
            var right = Right.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Left, left },
                { Right, right }
            };
            return new BinaryOpNode(
                ref idNext,
                Token.Clone(ts),
                SourceList.Clone(ts, newNodes),
                Op,
                left,
                right);
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Left.Accept(visitor);
                Right.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.BinaryOp;

        internal override BinaryOpNode CastBinaryOp()
        {
            return this;
        }

        internal override BinaryOpNode AsBinaryOp()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            if (Token.Kind == TokKind.PercentSign && Right.Token.Span.Lim < Left.Token.Span.Min)
            {
                return new Span(Right.Token.Span.Min, Left.Token.Span.Lim);
            }
            else
            {
                return new Span(Left.VerifyValue().GetCompleteSpan().Min, Right.VerifyValue().GetCompleteSpan().Lim);
            }
        }
    }
}
