// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    /// <summary>
    /// Base class for all lexing tokens.
    /// </summary>
    [ThreadSafeImmutable]
    public abstract class Token : IEquatable<Token>
    {
        internal Token(TokKind tid, Span span)
        {
            Kind = tid;
            Span = span;
        }

        /// <summary>
        /// Kind of the token.
        /// </summary>
        public TokKind Kind { get; }

        /// <summary>
        /// Span of the token in the formula.
        /// </summary>
        public Span Span { get; }

        internal virtual bool IsDottedNamePunctuator => false;

        internal abstract Token Clone(Span ts);

        /// <summary>
        /// Asserts that the object is in fact of type T before casting.
        /// </summary>
        internal T As<T>()
            where T : Token
        {
            Contracts.Assert(this is T);
            return (T)this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind.ToString();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Kind ^ (int)0x7AFF9182;
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Determines whether the specified <see cref="Token" /> is equal to the current one.
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public virtual bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            // Ensure the tokens have the same kind
            return Kind == that.Kind;
        }
    }
}
