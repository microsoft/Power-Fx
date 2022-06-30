// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Token for lexing error.
    /// </summary>
    public class ErrorToken : Token
    {
        /// <summary>
        /// Errors description key.
        /// May produce null, if there is no available detail for this error token.
        /// </summary>
        public ErrorResourceKey? DetailErrorKey { get; }

        // Args for ErrorResourceKey("UnexpectedCharacterToken")'s format string used in UnexpectedCharacterTokenError/LexError inside Lexer.cs.
        internal object[] ResourceKeyFormatStringArgs { get; }

        internal ErrorToken(Span span)
            : this(span, null)
        {
        }

        internal ErrorToken(Span span, ErrorResourceKey? detailErrorKey)
            : base(TokKind.Error, span)
        {
            Contracts.AssertValueOrNull(detailErrorKey);

            DetailErrorKey = detailErrorKey;
        }

        internal ErrorToken(Span span, ErrorResourceKey? detailErrorKey, params object[] args)
            : base(TokKind.Error, span)
        {
            Contracts.AssertValueOrNull(detailErrorKey);
            Contracts.AssertValueOrNull(args);

            DetailErrorKey = detailErrorKey;
            ResourceKeyFormatStringArgs = args;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorToken"/> class.
        /// Copy Ctor for ErrorToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private ErrorToken(ErrorToken tok, Span newSpan)
            : this(newSpan, tok.DetailErrorKey)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new ErrorToken(this, ts);
        }

        /// <inheritdoc />
        public override bool Equals(Token other)
        {
            Contracts.AssertValue(other);

            if (other is not ErrorToken otherToken)
            {
                return false;
            }

            return DetailErrorKey?.Key == otherToken.DetailErrorKey?.Key && base.Equals(otherToken) && Enumerable.SequenceEqual(ResourceKeyFormatStringArgs, otherToken.ResourceKeyFormatStringArgs);
        }
    }
}
