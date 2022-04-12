// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    /// <summary>
    /// Token for a string literal.
    /// </summary>
    public class StrLitToken : Token
    {
        internal StrLitToken(string val, Span span)
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

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Value of the string literal.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Escapes a string value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeString(string value) => CharacterUtils.EscapeString(value); // TODO: EscapeString vs. ExcelEscapeString?

        internal override Token Clone(Span ts)
        {
            return new StrLitToken(this, ts);
        }

        /// <inheritdoc />
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
