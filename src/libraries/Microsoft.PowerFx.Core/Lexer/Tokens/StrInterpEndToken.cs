// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class StrInterpEndToken : Token
    {
        public StrInterpEndToken(Span span)
            : base(TokKind.StrInterpEnd, span)
        {
        }

        public override string ToString()
        {
            return "\"";
        }

        internal override Token Clone(Span ts)
        {
            return new StrInterpEndToken(ts);
        }

        public override bool Equals(Token that)
        {
            Contracts.AssertValue(that);

            if (that is not StrInterpEndToken)
            {
                return false;
            }

            return base.Equals(that);
        }
    }
}
