// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class ErrorToken : Token
    {
        // May produce null, if there is no available detail for this error token.
        public ErrorResourceKey? DetailErrorKey { get; }

        // Args for ErrorResourceKey("UnexpectedCharacterToken")'s format string used in UnexpectedCharacterTokenError/LexError inside Lexer.cs.
        public object[] ResourceKeyFormatStringArgs { get; }

        public ErrorToken(Span span)
            : this(span, null)
        {
        }

        public ErrorToken(Span span, ErrorResourceKey? detailErrorKey)
            : base(TokKind.Error, span)
        {
            Contracts.AssertValueOrNull(detailErrorKey);

            DetailErrorKey = detailErrorKey;
        }

        public ErrorToken(Span span, ErrorResourceKey? detailErrorKey, params object[] args)
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

        public override Token Clone(Span ts)
        {
            return new ErrorToken(this, ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (that is not ErrorToken other)
            {
                return false;
            }

            return DetailErrorKey?.Key == other.DetailErrorKey?.Key && base.Equals(other) && Enumerable.SequenceEqual(ResourceKeyFormatStringArgs, other.ResourceKeyFormatStringArgs);
        }
    }
}
