// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class FirstNameNode : NameNode
    {
        public readonly Identifier Ident;

        public bool IsLhs => Parent != null && Parent.AsDottedName() != null;

        public FirstNameNode(ref int idNext, Token tok, SourceList sourceList, Identifier ident)
            : base(ref idNext, tok, sourceList)
        {
            Contracts.AssertValue(ident);
            Contracts.Assert(ident.Namespace.IsRoot);

            Ident = ident;
        }

        public FirstNameNode(ref int idNext, Token tok, Identifier ident)
            : this(ref idNext, tok, new SourceList(tok), ident)
        {
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new FirstNameNode(ref idNext, Token.Clone(ts), Ident.Clone(ts));
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.FirstName;

        public override FirstNameNode CastFirstName()
        {
            return this;
        }

        public override FirstNameNode AsFirstName()
        {
            return this;
        }
    }
}