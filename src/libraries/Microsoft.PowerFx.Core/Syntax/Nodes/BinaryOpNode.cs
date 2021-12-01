// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class BinaryOpNode : TexlNode
    {
        public readonly TexlNode Left;
        public readonly TexlNode Right;
        public readonly BinaryOp Op;

        public BinaryOpNode(ref int idNext, Token primaryToken, SourceList sourceList, BinaryOp op, TexlNode left, TexlNode right)
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

        public override TexlNode Clone(ref int idNext, Span ts)
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

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind { get { return NodeKind.BinaryOp; } }

        public override BinaryOpNode CastBinaryOp()
        {
            return this;
        }

        public override BinaryOpNode AsBinaryOp()
        {
            return this;
        }

        public override Span GetCompleteSpan()
        {
            if (this.Token.Kind == TokKind.PercentSign && this.Right.Token.Span.Lim < this.Left.Token.Span.Min)
                return new Span(this.Right.Token.Span.Min, this.Left.Token.Span.Lim);
            else
                return new Span(this.Left.VerifyValue().GetCompleteSpan().Min, this.Right.VerifyValue().GetCompleteSpan().Lim);
        }
    }
}