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
    /// First name parse node. Example:
    /// 
    /// <code>Ident</code>
    /// </summary>
    public sealed class FirstNameNode : NameNode
    {
        /// <summary>
        ///  The identifier of the first name node.
        /// </summary>
        public Identifier Ident { get; }

        internal bool IsLhs => Parent != null && Parent.AsDottedName() != null;

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
