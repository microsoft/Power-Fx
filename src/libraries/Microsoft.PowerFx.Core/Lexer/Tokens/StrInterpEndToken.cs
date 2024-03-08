// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class StrInterpEndToken : Token, ITextFirstFlag
    {
        public bool IsTextFirst { get; }

        public StrInterpEndToken(Span span, bool isTextFirst)
            : base(TokKind.StrInterpEnd, span)
        {
            IsTextFirst = isTextFirst;
        }

        public override string ToString()
        {
            return "\"";
        }

        internal override Token Clone(Span ts)
        {
            return new StrInterpEndToken(ts, IsTextFirst);
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
