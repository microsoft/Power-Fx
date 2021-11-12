// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class StrInterpStartToken : Token
    {
        public StrInterpStartToken(Span span)
            : base(TokKind.StrInterpStart, span)
        {
        }

        public override string ToString()
        {
            return "$\"";
        }

        public override Token Clone(Span ts)
        {
            return new StrInterpStartToken(ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is StrInterpStartToken))
                return false;
            return base.Equals(that);
        }
    }
}
