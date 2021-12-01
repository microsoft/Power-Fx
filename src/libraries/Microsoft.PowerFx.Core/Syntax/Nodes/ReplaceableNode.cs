// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class ReplaceableNode : TexlNode
    {
        public ReplaceableNode(ref int idNext, ReplaceableToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            Value = tok.Value;
            Contracts.AssertValue(Value);
        }

        public string Value { get; }

        public override NodeKind Kind { get; } = NodeKind.Replaceable;

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            return new ReplaceableNode(ref idNext, Token.Clone(ts).As<ReplaceableToken>());
        }

        public override ReplaceableNode AsReplaceable()
        {
            return this;
        }

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }
    }
}