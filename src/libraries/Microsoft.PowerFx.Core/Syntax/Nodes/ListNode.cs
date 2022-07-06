// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// List expression parse node. Example:
    /// 
    /// <code>[Arg1, Arg2, ...]</code>
    /// </summary>
    public sealed class ListNode : VariadicBase
    {
        internal readonly Token[] Delimiters;

        // Assumes ownership of 'args' array and the rgtokDelimiters array.
        internal ListNode(ref int idNext, Token tok, TexlNode[] args, Token[] delimiters, SourceList sourceList)
            : base(ref idNext, tok, sourceList, args)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValueOrNull(delimiters);
            Delimiters = delimiters;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var children = CloneChildren(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>();
            for (var i = 0; i < Children.Length; ++i)
            {
                newNodes.Add(Children[i], children[i]);
            }

            return new ListNode(
                ref idNext,
                Token.Clone(ts),
                children,
                Clone(Delimiters, ts),
                SourceList.Clone(ts, newNodes));
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
        public override NodeKind Kind => NodeKind.List;

        internal override ListNode CastList()
        {
            return this;
        }

        internal override ListNode AsList()
        {
            return this;
        }
    }
}
