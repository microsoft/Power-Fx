// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    public sealed class FirstNameNode : NameNode
    {
        public Identifier Ident { get; }

        public bool IsLhs => Parent != null && Parent.AsDottedName() != null;

        internal FirstNameNode(ref int idNext, Token tok, SourceList sourceList, Identifier ident)
            : base(ref idNext, tok, sourceList)
        {
            Contracts.AssertValue(ident);
            Contracts.Assert(ident.Namespace.IsRoot);

            Ident = ident;
        }

        internal FirstNameNode(ref int idNext, Token tok, Identifier ident)
            : this(ref idNext, tok, new SourceList(tok), ident)
        {
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new FirstNameNode(ref idNext, Token.Clone(ts), Ident.Clone(ts));
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
        public override NodeKind Kind => NodeKind.FirstName;

        internal override FirstNameNode CastFirstName()
        {
            return this;
        }

        internal override FirstNameNode AsFirstName()
        {
            return this;
        }
    }
}
