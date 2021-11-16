// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class IdentToken : Token
    {
        public readonly bool HasDelimiterStart;
        public readonly bool HasDelimiterEnd;
        public readonly bool IsModified;
        public readonly bool IsReplaceable;
        // Unescaped, unmodified value.
        private readonly string _value;
        public readonly DName Name;

        public IdentToken(string val, Span span)
            : this(val, span, false, false)
        {
            Contracts.AssertValue(val);
            //Contracts.Assert(val.Length == span.Lim - span.Min);
            _value = val;
            Name = DName.MakeValid(val, out IsModified);
        }

        public IdentToken(string val, Span spanTok, bool fDelimiterStart, bool fDelimiterEnd)
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

        public IdentToken(ReplaceableToken tok)
            : this(tok.Value, tok.Span)
        {
            IsReplaceable = true;
        }

        /// <summary>
        /// Copy Ctor for IdentToken used by Clone
        /// </summary>
        /// <param name="tok">The token to be copied</param>
        /// <param name="newSpan">The new span</param>
        private IdentToken(IdentToken tok, Span newSpan)
            : this(tok._value, newSpan, tok.HasDelimiterStart, tok.HasDelimiterEnd)
        {
        }

        public override Token Clone(Span ts)
        {
            return new IdentToken(this, ts);
        }

        // REVIEW ragru: having a property for every possible error isn't scalable.
        public bool HasDelimiters { get { return HasDelimiterStart; } }
        public bool HasErrors { get { return IsModified || (HasDelimiterStart && !HasDelimiterEnd); } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Format(sb);
            return sb.ToString();
        }

        // Prints the original string.
        public void Format(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            if (string.IsNullOrEmpty(_value))
            {
                sb.Append(TexlLexer.IdentifierDelimiter);
                sb.Append(TexlLexer.IdentifierDelimiter);
                return;
            }

            if (HasDelimiterStart)
                sb.Append(TexlLexer.IdentifierDelimiter);

            for (int i = 0; i < _value.Length; i++)
            {
                char ch = _value[i];
                sb.Append(ch);

                if (ch == TexlLexer.IdentifierDelimiter)
                    sb.Append(ch);
            }

            if (HasDelimiterEnd)
                sb.Append(TexlLexer.IdentifierDelimiter);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is IdentToken))
                return false;
            return Name == that.As<IdentToken>().Name && base.Equals(that);
        }
    }
}
