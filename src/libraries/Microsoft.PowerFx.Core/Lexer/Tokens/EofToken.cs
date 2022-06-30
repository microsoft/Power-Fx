// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    internal class EofToken : Token
    {
        public EofToken(Span span)
            : base(TokKind.Eof, span)
        {
        }

        internal override Token Clone(Span ts)
        {
            return new EofToken(ts);
        }
    }
}
