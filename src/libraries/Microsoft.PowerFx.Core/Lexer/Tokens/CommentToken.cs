// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer.Tokens
{
    // TODO: Docs
    public sealed class CommentToken : Token
    {
        internal bool IsOpenBlock;

        internal CommentToken(string val, Span span)
            : base(TokKind.Comment, span)
        {
            Contracts.AssertValue(val);
            Value = val;
        }

        /// <summary>
        /// Content of the comment.
        /// </summary>
        public string Value { get; }

        internal override Token Clone(Span ts)
        {
            return new CommentToken(Value, ts);
        }
    }
}
