﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    public class DecLitToken : Token
    {
        internal DecLitToken(decimal value, Span span)
            : base(TokKind.DecLit, span)
        {
            Contracts.Assert(value >= decimal.MinValue && value <= decimal.MaxValue);

            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecLitToken"/> class.
        /// Copy Ctor for NumLitToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private DecLitToken(DecLitToken tok, Span newSpan)
            : this(tok.Value, newSpan)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new DecLitToken(this, ts);
        }

        /// <summary>
        /// Numeric value of the token.
        /// </summary>
        public decimal Value { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            // $$$ can't use current culture
            return Value.ToString("G29", CultureInfo.CurrentCulture);
        }

        /// <inheritdoc />
        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is DecLitToken))
            {
                return false;
            }

            var thatNumLitToken = that.As<DecLitToken>();
            return Value == thatNumLitToken.Value && base.Equals(that);
        }
    }
}
