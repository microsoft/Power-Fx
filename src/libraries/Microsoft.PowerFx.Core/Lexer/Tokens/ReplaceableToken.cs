// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class ReplaceableToken : Token
    {
        public ReplaceableToken(string val, Span span)
            : base(TokKind.ReplaceableLit, span)
        {
            Contracts.AssertValue(val);
            Value = val;
        }

        protected ReplaceableToken(string value, TokKind kind, Span span)
            : base(kind, span)
        {
            Contracts.AssertValue(value);
            Value = value;
        }

        protected ReplaceableToken(ReplaceableToken tok, Span newSpan)
            : this(tok.Value, newSpan)
        {
        }

        public string Value { get; }

        public override string ToString() => Value;

        public override Token Clone(Span ts) => new ReplaceableToken(this, ts);

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is ReplaceableToken))
                return false;

            return Value == that.As<ReplaceableToken>().Value && base.Equals(that);
        }
    }
}