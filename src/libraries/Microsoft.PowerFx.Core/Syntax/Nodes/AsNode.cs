// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// As operator parse node. Example:
    /// 
    /// <code>Left As Identifier</code>
    /// </summary>
    public sealed class AsNode : TexlNode
    {
        /// <summary>
        /// Left operand of the as operator.
        /// </summary>
        public TexlNode Left { get; }

        /// <summary>
        /// Right operand (identifier) of the as operator.
        /// </summary>
        public Identifier Right { get; }

        internal AsNode(ref int idNext, Token primaryToken, SourceList sourceList, TexlNode left, Identifier right)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(left);
            Contracts.AssertValue(right);
            Left = left;
            Left.Parent = this;
            Right = right;
            _depth = 1 + left.Depth;

            MinChildID = left.MinChildID;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var left = Left.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Left, left },
            };

            return new AsNode(
                ref idNext,
                Token.Clone(ts),
                SourceList.Clone(ts, newNodes),
                left,
                Right);
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Left.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.As;

        internal override AsNode AsAsNode()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            return new Span(Left.GetCompleteSpan().Min, Right.Token.Span.Lim);
        }
    }
}
