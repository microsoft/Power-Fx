// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class StrInterpEndToken : Token
    {
        internal StrInterpEndToken(Span span, TokKind kind)
            : base(kind, span)
        {
        }

        public override string ToString()
        {
            return "\"";
        }

        internal override Token Clone(Span ts)
        {
            return new StrInterpEndToken(ts, Kind);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is StrInterpEndToken))
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
