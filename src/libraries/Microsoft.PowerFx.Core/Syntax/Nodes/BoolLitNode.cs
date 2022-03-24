// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    public sealed class BoolLitNode : TexlNode
    {
        internal BoolLitNode(ref int idNext, Token tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Contracts.AssertValue(tok);
            Contracts.Assert(tok.Kind == TokKind.True || tok.Kind == TokKind.False);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new BoolLitNode(ref idNext, Token.Clone(ts));
        }

        public bool Value => Token.Kind == TokKind.True;

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
        public override NodeKind Kind { get; } = NodeKind.BoolLit;

        internal override BoolLitNode CastBoolLit()
        {
            return this;
        }

        internal override BoolLitNode AsBoolLit()
        {
            return this;
        }
    }
}
