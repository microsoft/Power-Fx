﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Token for a comment.
    /// </summary>
    public sealed class CommentToken : Token
    {
        internal readonly bool IsOpenBlock;

        internal CommentToken(string val, Span span, bool isOpenBlock = false)
            : base(TokKind.Comment, span)
        {
            Contracts.AssertValue(val);
            Value = val;
            IsOpenBlock = isOpenBlock;
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
