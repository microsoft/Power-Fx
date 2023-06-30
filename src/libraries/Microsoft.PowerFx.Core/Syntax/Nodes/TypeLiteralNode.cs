// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    // CallNode :: Type ( TypeLiteralNode )
    // CallNode :: IsType ( TypeLiteralNode )
    // CallNode :: AsType ( Expr, TypeLiteralNode )

    public sealed class TypeLiteralNode : TexlNode
    {
        internal DType Type { get; }

        internal TypeLiteralNode(ref int idNext, Token firstToken, DType type, SourceList sources)
            : base(ref idNext, firstToken, sources)
        {
            Type = type;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new TypeLiteralNode(ref idNext, Token.Clone(ts).As<Token>(), Type, this.SourceList.Clone(ts, new Dictionary<TexlNode, TexlNode>()));
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
