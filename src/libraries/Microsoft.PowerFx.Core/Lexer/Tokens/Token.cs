// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal abstract class Token : IEquatable<Token>
    {
        public Token(TokKind tid, Span span)
        {
            Kind = tid;
            Span = span;
        }

        public TokKind Kind { get; }

        public Span Span { get; }

        public virtual bool IsDottedNamePunctuator => false;

        public abstract Token Clone(Span ts);

        /// <summary>
        /// Asserts that the object is in fact of type T before casting.
        /// </summary>
        public T As<T>()
            where T : Token
        {
            Contracts.Assert(this is T);
            return (T)this;
        }

        public override string ToString()
        {
            return Kind.ToString();
        }

        public override int GetHashCode()
        {
            return (int)Kind ^ (int)0x7AFF9182;
        }

        public override bool Equals(object that)
        {
            if (that == null)
            {
                return false;
            }

            if (!(that is Token))
            {
                return false;
            }

            return Equals((Token)that);
        }

        public virtual bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            // Ensure the tokens have the same kind
            return Kind == that.Kind;
        }
    }
}