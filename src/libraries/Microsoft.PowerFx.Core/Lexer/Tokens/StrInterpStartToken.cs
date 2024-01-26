// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Syntax
{
    internal class StrInterpStartToken : Token
    {
        internal StrInterpStartToken(Span span, TokKind kind)
            : base(kind, span)
        {
        }

        public override string ToString()
        {
            return "$\"";
        }

        internal override Token Clone(Span ts)
        {
            return new StrInterpStartToken(ts, Kind);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (!(that is StrInterpStartToken))
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
