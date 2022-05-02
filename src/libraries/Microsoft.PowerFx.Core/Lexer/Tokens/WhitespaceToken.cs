// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// A token for a series of whitespace characters.
    /// </summary>
    internal class WhitespaceToken : Token
    {
        public string Value { get; }

        public WhitespaceToken(string value, Span span)
            : base(TokKind.Whitespace, span)
        {
            Contracts.AssertValue(value);
            Value = value;
        }

        internal override Token Clone(Span ts)
        {
            return new WhitespaceToken(Value, ts);
        }
    }
}
