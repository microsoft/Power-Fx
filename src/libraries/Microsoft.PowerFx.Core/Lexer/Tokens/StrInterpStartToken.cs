// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
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

        internal override Token Clone(Span ts)
        {
            return new StrInterpStartToken(ts);
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
