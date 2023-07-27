// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    // CallNode :: Type ( TypeLiteralNode )
    // CallNode :: IsType ( Expr, TypeLiteralNode )
    // CallNode :: AsType ( Expr, TypeLiteralNode )
    // TypeLiteralNode will allow us to store information about a new kind of syntax, that being the Type Literal syntax, and manipulate it.
    // TypeLiteral syntax needs to *only* work for these partial-compile time functions that evaluate the Type Literal syntax into some sort of 
    // Representation so we can evaluate and compare it. We want to make sure that this Type Literal information isn't able to be
    // Passed around in undesirable ways be users of PowerFx. A TypeLiteralNode allows us to strictly enforce requirements.

    public sealed class TypeLiteralNode : TexlNode
    {
        internal TypeLiteralNode(ref int idNext, Token firstToken, SourceList sources)
            : base(ref idNext, firstToken, sources)
        {
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new TypeLiteralNode(ref idNext, Token.Clone(ts).As<Token>(), this.SourceList.Clone(ts, new Dictionary<TexlNode, TexlNode>()));
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
