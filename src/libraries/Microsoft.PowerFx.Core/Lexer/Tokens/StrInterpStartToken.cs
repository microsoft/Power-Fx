// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class StrInterpStartToken : Token, ITextFirstFlag
    {
        public bool IsTextFirst { get; }

        public StrInterpStartToken(Span span, bool isTextFirst)
            : base(TokKind.StrInterpStart, span)
        {
            IsTextFirst = isTextFirst;
        }

        public override string ToString()
        {
            return "$\"";
        }

        internal override Token Clone(Span ts)
        {
            return new StrInterpStartToken(ts, IsTextFirst);
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
