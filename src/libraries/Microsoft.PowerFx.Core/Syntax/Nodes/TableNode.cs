// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class TableNode : VariadicBase
    {
        public readonly Token[] Commas;
        // BracketClose can be null.
        public readonly Token BracketClose;

        // Assumes ownership of all of the array args.
        public TableNode(ref int idNext, Token primaryToken, SourceList sourceList, TexlNode[] exprs, Token[] commas, Token bracketCloseToken)
            : base(ref idNext, primaryToken, sourceList, exprs)
        {
            Contracts.AssertValue(exprs);
            Contracts.AssertValueOrNull(commas);
            Contracts.AssertValueOrNull(bracketCloseToken);

            Commas = commas;
            BracketClose = bracketCloseToken;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
                newNodes.Add(Children[i], children[i]);

            return new TableNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), children, Clone(Commas, ts), BracketClose.Clone(ts));
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

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.Table;

        public override TableNode AsTable()
        {
            return this;
        }

        public override Span GetCompleteSpan()
        {
            int lim;
            if (BracketClose != null)
                lim = BracketClose.Span.Lim;
            else if (Children.Count() == 0)
                lim = Token.VerifyValue().Span.Lim;
            else
                lim = Children.VerifyValue().Last().VerifyValue().GetCompleteSpan().Lim;

            return new Span(Token.VerifyValue().Span.Min, lim);
        }

        public override Span GetTextSpan()
        {
            var lim = BracketClose == null ? Token.VerifyValue().Span.Lim : BracketClose.Span.Lim;
            return new Span(Token.VerifyValue().Span.Min, lim);
        }
    }
}