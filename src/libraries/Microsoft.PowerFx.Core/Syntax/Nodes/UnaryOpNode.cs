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
    internal sealed class UnaryOpNode : TexlNode
    {
        public readonly TexlNode Child;
        public readonly UnaryOp Op;

        public bool IsPercent => Op == UnaryOp.Percent;

        public UnaryOpNode(ref int idNext, Token primaryToken, SourceList sourceList, UnaryOp op, TexlNode child)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(child);
            Child = child;
            Child.Parent = this;
            Op = op;
            _depth = child.Depth + 1;
            MinChildID = Math.Min(child.MinChildID, MinChildID);
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var child = Child.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Child, child }
            };
            return new UnaryOpNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), Op, child);
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Child.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.UnaryOp;

        public override UnaryOpNode CastUnaryOp()
        {
            return this;
        }

        public override UnaryOpNode AsUnaryOpLit()
        {
            return this;
        }

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