// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Parent identifier parse node.
    /// </summary>
    public sealed class ParentNode : NameNode
    {
        internal ParentNode(ref int idNext, Token tok)
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
            return new ParentNode(ref idNext, Token.Clone(ts));
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.Parent;

        internal override ParentNode AsParent()
        {
            return this;
        }
    }
}
