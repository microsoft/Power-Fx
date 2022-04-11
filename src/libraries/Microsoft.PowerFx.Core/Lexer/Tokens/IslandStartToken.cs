// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class IslandStartToken : Token
    {
        public IslandStartToken(Span span)
            : base(TokKind.IslandStart, span)
        {
        }

        public override string ToString()
        {
            return "{";
        }

        internal override Token Clone(Span ts)
        {
            return new IslandStartToken(ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is IslandStartToken))
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
