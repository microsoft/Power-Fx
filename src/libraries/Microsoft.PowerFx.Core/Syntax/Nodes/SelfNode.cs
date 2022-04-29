// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    /// <summary>
    /// Self identifier parse node.
    /// </summary>
    public sealed class SelfNode : NameNode
    {
        internal SelfNode(ref int idNext, Token tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new SelfNode(ref idNext, Token.Clone(ts));
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.Self;

        internal override SelfNode AsSelf()
        {
            return this;
        }
    }
}
