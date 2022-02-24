﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class StrInterpNode : VariadicBase
    {
        // StrInterpEnd can be null.
        public readonly Token StrInterpEnd;

        public StrInterpNode(ref int idNext, Token strInterpStart, SourceList sourceList, TexlNode[] children, Token strInterpEnd)
            : base(ref idNext, strInterpStart, sourceList, children)
        {
            Contracts.AssertValueOrNull(strInterpEnd);
            StrInterpEnd = strInterpEnd;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            return new StrInterpNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), children, StrInterpEnd);
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                AcceptChildren(visitor);
                visitor.PostVisit(this);
            }
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.StrInterp;

        public override StrInterpNode AsStrInterp()
        {
            return this;
        }

        public override Span GetTextSpan()
        {
            if (StrInterpEnd == null)
            {
                return base.GetTextSpan();
            }

            return new Span(Token.Span.Min, StrInterpEnd.Span.Lim);
        }

        public override Span GetCompleteSpan()
        {
            int limit;

            // If we have a close paren, then the call node is complete.
            // If not, then the call node ends with the end of the last argument.
            if (StrInterpEnd != null)
            {
                limit = StrInterpEnd.Span.Lim;
            }
            else
            {
                limit = Children.Last().GetCompleteSpan().Lim;
            }

            return new Span(Token.Span.Min, limit);
        }
    }
}
