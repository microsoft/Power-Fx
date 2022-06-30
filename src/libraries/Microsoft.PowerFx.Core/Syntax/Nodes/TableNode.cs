// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Table expression parse node. Example:
    /// 
    /// <code>[E1, ...]</code>
    /// </summary>
    public sealed class TableNode : VariadicBase
    {
        internal readonly Token[] Commas;

        // BracketClose can be null.
        internal readonly Token BracketClose;

        // Assumes ownership of all of the array args.
        internal TableNode(ref int idNext, Token primaryToken, SourceList sourceList, TexlNode[] exprs, Token[] commas, Token bracketCloseToken)
            : base(ref idNext, primaryToken, sourceList, exprs)
        {
            Contracts.AssertValue(exprs);
            Contracts.AssertValueOrNull(commas);
            Contracts.AssertValueOrNull(bracketCloseToken);

            Commas = commas;
            BracketClose = bracketCloseToken;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            return new TableNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), children, Clone(Commas, ts), BracketClose.Clone(ts));
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                AcceptChildren(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.Table;

        internal override TableNode AsTable()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            int lim;
            if (BracketClose != null)
            {
                lim = BracketClose.Span.Lim;
            }
            else if (Children.Length == 0)
            {
                lim = Token.VerifyValue().Span.Lim;
            }
            else
            {
                lim = Children.VerifyValue().Last().VerifyValue().GetCompleteSpan().Lim;
            }

            return new Span(Token.VerifyValue().Span.Min, lim);
        }

        /// <inheritdoc />
        public override Span GetTextSpan()
        {
            var lim = BracketClose == null ? Token.VerifyValue().Span.Lim : BracketClose.Span.Lim;
            return new Span(Token.VerifyValue().Span.Min, lim);
        }
    }
}
