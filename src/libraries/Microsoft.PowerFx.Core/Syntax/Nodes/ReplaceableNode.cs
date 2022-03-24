// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    public sealed class ReplaceableNode : TexlNode
    {
        internal ReplaceableNode(ref int idNext, ReplaceableToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Value = tok.Value;
            Contracts.AssertValue(Value);
        }

        public string Value { get; }

        /// <inheritdoc />
        public override NodeKind Kind { get; } = NodeKind.Replaceable;

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            return new ReplaceableNode(ref idNext, Token.Clone(ts).As<ReplaceableToken>());
        }

        internal override ReplaceableNode AsReplaceable()
        {
            return this;
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
    }
}
