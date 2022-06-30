// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Record expression parse node.
    /// 
    /// <code>{X1: E1, X2: E2, ...}</code>
    /// </summary>
    public sealed class RecordNode : VariadicBase
    {
        internal readonly Token[] Commas;
        internal readonly Token[] Colons;

        /// <summary>
        /// The record identifier names (i.e., field names).
        /// </summary>
        public IReadOnlyList<Identifier> Ids { get; }

        // CurlyClose can be null.
        internal readonly Token CurlyClose;

        // SourceRestriction can be null
        // Used to associate a record that is using display names with a data source
        internal readonly TexlNode SourceRestriction;

        // Assumes ownership of all of the array args.
        internal RecordNode(ref int idNext, Token primaryTokens, SourceList sourceList, Identifier[] ids, TexlNode[] exprs, Token[] commas, Token[] colons, Token curlyCloseToken, TexlNode sourceRestriction = null)
            : base(ref idNext, primaryTokens, sourceList, exprs)
        {
            Contracts.AssertValue(ids);
            Contracts.AssertValue(exprs);
            Contracts.Assert(ids.Length == exprs.Length);
            Contracts.AssertValueOrNull(commas);
            Contracts.AssertValueOrNull(colons);
            Contracts.AssertValueOrNull(curlyCloseToken);
            Contracts.AssertValueOrNull(sourceRestriction);

            Ids = ids;
            Commas = commas;
            Colons = colons;
            CurlyClose = curlyCloseToken;
            SourceRestriction = sourceRestriction;
            if (sourceRestriction != null)
            {
                sourceRestriction.Parent = this;
                MinChildID = Math.Min(sourceRestriction.MinChildID, MinChildID);
            }
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            var newIdentifiers = new Identifier[Ids.Count];
            for (var x = 0; x < Ids.Count; x++)
            {
                newIdentifiers[x] = Ids[x].Clone(ts);
            }

            return new RecordNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), newIdentifiers, children, Clone(Commas, ts), Clone(Colons, ts), CurlyClose.Clone(ts), SourceRestriction?.Clone(ref idNext, ts));
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                if (SourceRestriction != null)
                {
                    SourceRestriction.Accept(visitor);
                }

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
        public override NodeKind Kind => NodeKind.Record;

        internal override RecordNode CastRecord()
        {
            return this;
        }

        internal override RecordNode AsRecord()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetTextSpan()
        {
            var lim = CurlyClose == null ? Token.VerifyValue().Span.Lim : CurlyClose.Span.Lim;
            return new Span(Token.VerifyValue().Span.Min, lim);
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            return new Span(GetTextSpan());
        }
    }
}
