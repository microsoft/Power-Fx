// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class CommentToken : Token
    {
        public bool IsOpenBlock;

        public CommentToken(string val, Span span)
            : base(TokKind.Comment, span)
        {
            Contracts.AssertValue(val);
            Value = val;
        }

        public string Value { get; }

        public override Token Clone(Span ts)
        {
            return new CommentToken(Value, ts);
        }
    }
}