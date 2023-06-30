// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    public sealed class TypeLiteralNode : TexlNode
    {
        internal TypeLiteralNode(ref int idNext, IdentToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {   
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new TypeLiteralNode(ref idNext, Token.Clone(ts).As<IdentToken>());
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

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.TypeLiteral;
    }
}
