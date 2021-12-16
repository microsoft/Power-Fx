// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class NumLitToken : Token
    {
        public NumLitToken(double value, Span span)
            : base(TokKind.NumLit, span)
        {
            Contracts.Assert(double.MinValue <= value && value < double.MaxValue);

            Value = value;
        }

        /// <summary>
        /// Copy Ctor for NumLitToken used by Clone
        /// </summary>
        /// <param name="tok">The token to be copied</param>
        /// <param name="newSpan">The new span</param>
        private NumLitToken(NumLitToken tok, Span newSpan)
            : this(tok.Value, newSpan)
        {
        }

        public override Token Clone(Span ts)
        {
            return new NumLitToken(this, ts);
        }

        public double Value { get; }

        public override string ToString()
        {
            return Value.ToString("R", TexlLexer.LocalizedInstance.Culture);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is NumLitToken))
                return false;
            NumLitToken thatNumLitToken = that.As<NumLitToken>();
            return Value == thatNumLitToken.Value && base.Equals(that);
        }
    }
}