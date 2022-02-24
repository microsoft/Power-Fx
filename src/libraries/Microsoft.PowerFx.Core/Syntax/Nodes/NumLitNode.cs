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
    internal sealed class NumLitNode : TexlNode
    {
        // If Value is non-null, then the token represents its value.
        // Otherwise, the value is in NumValue.
        public readonly double NumValue;

        public NumLitNode(ref int idNext, NumLitToken tok)
            : base(ref idNext, tok, new SourceList(tok))
        {
            NumValue = double.NaN;
        }

        public NumLitNode(ref int idNext, Token tok, SourceList sourceList, double value)
            : base(ref idNext, tok, sourceList)
        {
            Contracts.Assert(tok.Kind != TokKind.NumLit);
            NumValue = value;
        }

        public override TexlNode Clone(ref int idNext, Span ts)
        {
            if (Value == null)
            {
                return new NumLitNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, null), NumValue);
            }

            return new NumLitNode(ref idNext, Value.Clone(ts).As<NumLitToken>());
        }

        // This may be null, in which case, NumValue should be used.
        public NumLitToken Value => Token as NumLitToken;

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.NumLit;

        public override NumLitNode AsNumLit()
        {
            return this;
        }
    }
}