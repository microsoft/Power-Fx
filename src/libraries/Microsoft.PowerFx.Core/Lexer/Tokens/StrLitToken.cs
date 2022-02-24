// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class StrLitToken : Token
    {
        public StrLitToken(string val, Span span)
            : base(TokKind.StrLit, span)
        {
            Contracts.AssertValue(val);
            Value = val;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StrLitToken"/> class.
        /// Copy Ctor for StrLitToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private StrLitToken(StrLitToken tok, Span newSpan)
            : this(tok.Value, newSpan)
        {
        }

        public override string ToString()
        {
            return Value;
        }

        public string Value { get; }

        public override Token Clone(Span ts)
        {
            return new StrLitToken(this, ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is StrLitToken))
            {
                return false;
            }

            return Value == that.As<StrLitToken>().Value && base.Equals(that);
        }
    }
}