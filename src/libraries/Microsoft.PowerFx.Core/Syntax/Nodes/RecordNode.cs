// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class RecordNode : VariadicBase
    {
        public readonly Token[] Commas;
        public readonly Token[] Colons;
        public readonly Identifier[] Ids;

        // CurlyClose can be null.
        public readonly Token CurlyClose;

        // SourceRestriction can be null
        // Used to associate a record that is using display names with a data source
        public readonly TexlNode SourceRestriction;

        // Assumes ownership of all of the array args.
        public RecordNode(ref int idNext, Token primaryTokens, SourceList sourceList, Identifier[] ids, TexlNode[] exprs, Token[] commas, Token[] colons, Token curlyCloseToken, TexlNode sourceRestriction = null)
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

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            var newIdentifiers = new Identifier[Ids.Length];
            for (var x = 0; x < Ids.Length; x++)
            {
                newIdentifiers[x] = Ids[x].Clone(ts);
            }

            return new RecordNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), newIdentifiers, children, Clone(Commas, ts), Clone(Colons, ts), CurlyClose.Clone(ts), SourceRestriction?.Clone(ref idNext, ts));
        }

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

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.Record;

        public override RecordNode CastRecord()
        {
            return this;
        }

        public override RecordNode AsRecord()
        {
            return this;
        }

        public override Span GetTextSpan()
        {
            var lim = CurlyClose == null ? Token.VerifyValue().Span.Lim : CurlyClose.Span.Lim;
            return new Span(Token.VerifyValue().Span.Min, lim);
        }

        public override Span GetCompleteSpan()
        {
            return new Span(GetTextSpan());
        }
    }
}