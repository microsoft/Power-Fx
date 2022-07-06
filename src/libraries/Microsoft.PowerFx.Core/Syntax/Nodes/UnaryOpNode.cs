// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Unary operation parse node. Examples:
    /// 
    /// <code>Op Child</code>
    /// <code>Child %</code>
    /// </summary>
    public sealed class UnaryOpNode : TexlNode
    {
        /// <summary>
        /// The unary operation operand.
        /// </summary>
        public TexlNode Child { get; }

        /// <summary>
        /// The unary operator.
        /// </summary>
        public UnaryOp Op { get; }

        internal bool IsPercent => Op == UnaryOp.Percent;

        internal UnaryOpNode(ref int idNext, Token primaryToken, SourceList sourceList, UnaryOp op, TexlNode child)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(child);
            Child = child;
            Child.Parent = this;
            Op = op;
            _depth = child.Depth + 1;
            MinChildID = Math.Min(child.MinChildID, MinChildID);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var child = Child.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Child, child }
            };
            return new UnaryOpNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), Op, child);
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Child.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.UnaryOp;

        internal override UnaryOpNode CastUnaryOp()
        {
            return this;
        }

        internal override UnaryOpNode AsUnaryOpLit()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            // For syntax coloring regarding percentages
            if (IsPercent)
            {
                return new Span(Child.Token.VerifyValue().Span.Min, Child.Token.VerifyValue().Span.Lim + TexlLexer.PunctuatorPercent.Length);
            }
            else
            {
                return new Span(Token.VerifyValue().Span.Min, Child.VerifyValue().GetCompleteSpan().Lim);
            }
        }
    }
}
