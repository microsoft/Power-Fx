// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Token for a numeric literal.
    /// </summary>
    public class UnitsPreSymbolToken : Token
    {
        public string Symbol { get; }

        internal UnitsPreSymbolToken(string symbol, Span span)
            : base(TokKind.UnitPreSymbol, span)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitsPreSymbolToken"/> class.
        /// Copy Ctor for UnitsPreSymbolToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private UnitsPreSymbolToken(UnitsPreSymbolToken tok, Span newSpan)
            : this(tok.Symbol, newSpan)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new UnitsPreSymbolToken(this, ts);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // $$$ can't use current culture
            return Symbol;
        }

        /// <inheritdoc />
        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is UnitsPreSymbolToken))
            {
                return false;
            }

            var thatPreSymbolToken = that.As<UnitsPreSymbolToken>();
            return Symbol == thatPreSymbolToken.Symbol && base.Equals(that);
        }
    }

    /// <summary>
    /// Token for a numeric literal.
    /// </summary>
    public class UnitsPostSymbolToken : Token
    {
        public string Symbol { get; }

        internal UnitsPostSymbolToken(string symbol, Span span)
            : base(TokKind.UnitPostSymbol, span)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitsPostSymbolToken"/> class.
        /// Copy Ctor for UnitsPostSymbolToken used by Clone.
        /// </summary>
        /// <param name="tok">The token to be copied.</param>
        /// <param name="newSpan">The new span.</param>
        private UnitsPostSymbolToken(UnitsPostSymbolToken tok, Span newSpan)
            : this(tok.Symbol, newSpan)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new UnitsPostSymbolToken(this, ts);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // $$$ can't use current culture
            return Symbol;
        }

        /// <inheritdoc />
        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is UnitsPreSymbolToken))
            {
                return false;
            }

            var thatPreSymbolToken = that.As<UnitsPreSymbolToken>();
            return Symbol == thatPreSymbolToken.Symbol && base.Equals(that);
        }
    }
}
