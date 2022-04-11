// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class IslandEndToken : Token
    {
        public IslandEndToken(Span span)
            : base(TokKind.IslandEnd, span)
        {
        }

        public override string ToString()
        {
            return "}";
        }

        internal override Token Clone(Span ts)
        {
            return new IslandEndToken(ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is IslandEndToken))
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
