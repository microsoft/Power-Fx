// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    internal class CommentToken : Token
    {
        private readonly string _val;
        public bool IsOpenBlock;

        public CommentToken(string val, Span span)
            : base(TokKind.Comment, span)
        {
            Contracts.AssertValue(val);
            _val = val;
        }

        public string Value { get { return _val; } }

        public override Token Clone(Span ts)
        {
            return new CommentToken(Value, ts);
        }
    }
}