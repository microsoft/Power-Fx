// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class BoolLitNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public BoolLitNodeSuggestionHandler()
                : base(NodeKind.BoolLit)
            { }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;
                var boolNode = curNode.CastBoolLit();
                var tokenSpan = curNode.Token.Span;

                if (cursorPos < tokenSpan.Min)
                {
                    // Cursor is before the token start.
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, curNode);
                }
                else if (cursorPos <= tokenSpan.Lim)
                {
                    // Cursor is in the middle of the token.
                    var replacementLength = tokenSpan.Min == cursorPos ? 0 : tokenSpan.Lim - tokenSpan.Min;
                    intellisenseData.SetMatchArea(tokenSpan.Min, cursorPos, replacementLength);
                    intellisenseData.BoundTo = boolNode.Value ? TexlLexer.KeywordTrue : TexlLexer.KeywordFalse;
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, curNode);
                }
                else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                {
                    // Verify that cursor is after a space after the current node's token.
                    // Suggest binary keywords.
                    IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, DType.Boolean);
                }

                return true;
            }
        }
    }
}