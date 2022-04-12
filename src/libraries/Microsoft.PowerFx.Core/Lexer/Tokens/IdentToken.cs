// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    /// <summary>
    /// Token for an identifier/name.
    /// </summary>
    public class IdentToken : Token
    {
        internal readonly bool HasDelimiterStart;
        internal readonly bool HasDelimiterEnd;
        internal readonly bool IsModified;

        // Unescaped, unmodified value.
        private readonly string _value;

        /// <summary>
        /// Identifier represented as <see cref="DName" />.
        /// </summary>
        public DName Name { get; }

        internal const string StrInterpIdent = "Concatenate";

        internal IdentToken(string val, Span span)
            : this(val, span, false, false)
        {
            Contracts.AssertValue(val);

            // String interpolation sometimes creates tokens that do not exist in the source code
            // so we skip validating the span length for the Ident that the parser generates
            Contracts.Assert(val.Length == span.Lim - span.Min || val == StrInterpIdent);
            _value = val;
            Name = DName.MakeValid(val, out IsModified);
        }

        internal IdentToken(string val, Span spanTok, bool fDelimiterStart, bool fDelimiterEnd)
            : base(TokKind.Ident, spanTok)
        {
            // The string may be empty, but shouldn't be null.
            Contracts.AssertValue(val);
            Contracts.Assert(fDelimiterStart || !fDelimiterEnd);

            _value = val;
            Name = DName.MakeValid(val, out IsModified);
            HasDelimiterStart = fDelimiterStart;
            HasDelimiterEnd = fDelimiterEnd;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentToken"/> class.
        /// Copy Ctor for IdentToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private IdentToken(IdentToken tok, Span newSpan)
            : this(tok._value, newSpan, tok.HasDelimiterStart, tok.HasDelimiterEnd)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new IdentToken(this, ts);
        }

        // REVIEW ragru: having a property for every possible error isn't scalable.
        internal bool HasDelimiters => HasDelimiterStart;

        /// <summary>
        /// Whether an identifier has errors.
        /// </summary>
        public bool HasErrors => IsModified || (HasDelimiterStart && !HasDelimiterEnd);

        // TODO: How to do this properly?
        public static string MakeValidIdentifier(string value)
        {
            var needsDelimiters = string.IsNullOrEmpty(value) || !value.All(TexlLexer.IsSimpleIdentCh);
            var tmpIdent = new IdentToken(value, new Span(0, 0), needsDelimiters, needsDelimiters);
            return tmpIdent.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            Format(sb);
            return sb.ToString();
        }

        // Prints the original string.
        internal void Format(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            if (string.IsNullOrEmpty(_value))
            {
                sb.Append(TexlLexer.IdentifierDelimiter);
                sb.Append(TexlLexer.IdentifierDelimiter);
                return;
            }

            if (HasDelimiterStart)
            {
                sb.Append(TexlLexer.IdentifierDelimiter);
            }

            for (var i = 0; i < _value.Length; i++)
            {
                var ch = _value[i];
                sb.Append(ch);

                if (ch == TexlLexer.IdentifierDelimiter)
                {
                    sb.Append(ch);
                }
            }

            if (HasDelimiterEnd)
            {
                sb.Append(TexlLexer.IdentifierDelimiter);
            }
        }

        /// <inheritdoc />
        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is IdentToken))
            {
                return false;
            }

            return Name == that.As<IdentToken>().Name && base.Equals(that);
        }
    }
}
