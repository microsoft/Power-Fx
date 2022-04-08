// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal partial class Intellisense
    {
        /// <summary>
        /// Suggests operators that can be used on a value of type record or table.  E.g. In, As. 
        /// </summary>
        internal sealed class RecordNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public RecordNodeSuggestionHandler()
                : base(NodeKind.Record)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var tokenSpan = curNode.Token.Span;

                // Only suggest after record nodes
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