﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class ParseResult
    {
        internal TexlNode Root { get; }

        internal List<TexlError> Errors { get; }

        internal bool HasError { get; }

        internal List<CommentToken> Comments { get; }

        internal SourceList Before { get; }

        internal SourceList After { get; }

        public ParseResult(TexlNode root, List<TexlError> errors, bool hasError, List<CommentToken> comments, SourceList before, SourceList after)
        {
            Contracts.AssertValue(root);
            Contracts.AssertValue(comments);
            // You can have an empty error list and still have a semi-silent error, but if you have an error in your list there must have been an error.
            Contracts.Assert(errors != null ? hasError : true);

            Root = root;
            Errors = errors;
            HasError = hasError;
            Comments = comments;
            Before = before;
            After = after;
        }
    }
}