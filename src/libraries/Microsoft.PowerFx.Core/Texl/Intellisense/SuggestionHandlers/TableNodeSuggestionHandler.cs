﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class TableNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public TableNodeSuggestionHandler()
                : base(NodeKind.Table)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var tokenSpan = curNode.Token.Span;

                // Only suggest after table nodes
                if (cursorPos <= tokenSpan.Lim)
                {
                    return true;
                }

                if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                {
                    // Verify that cursor is after a space after the current node's token.
                    // Suggest binary keywords.
                    IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, DType.EmptyTable);
                }

                return true;
            }
        }
    }
}
