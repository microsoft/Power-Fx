// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class StrInterpSuggestionHandler : NodeKindSuggestionHandler
        {
            public StrInterpSuggestionHandler()
                : base(NodeKind.StrInterp)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var strInterpNode = curNode.AsStrInterp();
                var spanMin = strInterpNode.Token.Span.Min;

                if (cursorPos < spanMin)
                {
                    // Cursor is before the head
                    // i.e. | $"...."
                    // Suggest possibilities that can result in a value.
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, strInterpNode);
                }
                else if (strInterpNode.Token.Span.Lim > cursorPos || strInterpNode.StrInterpEnd == null)
                {
                    // Handling the erroneous case when user enters a space after $ and cursor is after space.
                    // Cursor is before the open quote.
                    // Eg: $ | "
                    return false;
                }
                else
                {
                    // If there was no close quote we would have an error node.
                    Contracts.Assert(strInterpNode.StrInterpEnd != null);

                    if (cursorPos <= strInterpNode.StrInterpEnd.Span.Min)
                    {
                        IntellisenseHelper.AddSuggestionsForTopLevel(intellisenseData, strInterpNode);
                    }
                    else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                    {
                        IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, intellisenseData.Binding.GetType(strInterpNode));
                    }
                }

                return false;
            }
        }
    }
}
