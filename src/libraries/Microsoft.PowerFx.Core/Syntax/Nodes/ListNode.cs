// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class ListNode : VariadicBase
    {
        public readonly Token[] Delimiters;

        // Assumes ownership of 'args' array and the rgtokDelimiters array.
        public ListNode(ref int idNext, Token tok, TexlNode[] args, Token[] delimiters, SourceList sourceList)
            : base(ref idNext, tok, sourceList, args)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValueOrNull(delimiters);
            Delimiters = delimiters;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
                newNodes.Add(Children[i], children[i]);

            return new ListNode(
                ref idNext,
                Token.Clone(ts),
                children,
                Clone(Delimiters, ts),
                SourceList.Clone(ts, newNodes));
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

        public override NodeKind Kind => NodeKind.List;

        public override ListNode CastList()
        {
            return this;
        }

        public override ListNode AsList()
        {
            return this;
        }
    }
}