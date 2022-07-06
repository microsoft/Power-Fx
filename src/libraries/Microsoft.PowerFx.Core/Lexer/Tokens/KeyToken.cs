﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class KeyToken : Token
    {
        public KeyToken(TokKind tid, Span span)
            : base(tid, span)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyToken"/> class.
        /// Copy Ctor for KeyToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private KeyToken(KeyToken tok, Span newSpan)
            : this(tok.Kind, newSpan)
        {
        }

        internal override bool IsDottedNamePunctuator => Kind == TokKind.Dot || Kind == TokKind.Bang || Kind == TokKind.BracketOpen;

        internal override Token Clone(Span ts)
        {
            return new KeyToken(this, ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is KeyToken))
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
