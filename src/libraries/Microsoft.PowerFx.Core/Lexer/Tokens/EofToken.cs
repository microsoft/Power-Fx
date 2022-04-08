// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class EofToken : Token
    {
        public EofToken(Span span)
            : base(TokKind.Eof, span)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EofToken"/> class.
        /// Copy Ctor for EofToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private EofToken(EofToken tok, Span newSpan)
            : this(newSpan)
        {
        }

        public override Token Clone(Span ts)
        {
            return new EofToken(this, ts);
        }
    }
}
