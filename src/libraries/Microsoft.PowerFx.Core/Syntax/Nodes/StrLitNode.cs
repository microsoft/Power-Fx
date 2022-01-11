// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class StrLitNode : TexlNode
    {
        public readonly string Value;

        public StrLitNode(ref int idNext, StrLitToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Value = tok.Value;
            Contracts.AssertValue(Value);
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new StrLitNode(ref idNext, Token.Clone(ts).As<StrLitToken>());
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

        public override NodeKind Kind => NodeKind.StrLit;

        public override StrLitNode AsStrLit()
        {
            return this;
        }
    }
}