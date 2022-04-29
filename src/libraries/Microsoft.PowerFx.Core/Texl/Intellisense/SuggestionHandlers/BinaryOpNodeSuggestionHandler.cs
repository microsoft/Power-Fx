// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class BinaryOpNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public BinaryOpNodeSuggestionHandler()
                : base(NodeKind.BinaryOp)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;

                // Cursor is in the operation token.
                // Suggest binary operators.
                var binaryOpNode = curNode.CastBinaryOp();
                var tokenSpan = binaryOpNode.Token.Span;

                var keyword = binaryOpNode.Op == BinaryOp.Error ? tokenSpan.GetFragment(intellisenseData.Script) : TexlParser.GetTokString(binaryOpNode.Token.Kind);
                var replacementLength = tokenSpan.Min == intellisenseData.CursorPos ? 0 : tokenSpan.Lim - tokenSpan.Min;
                intellisenseData.SetMatchArea(tokenSpan.Min, intellisenseData.CursorPos, replacementLength);
                intellisenseData.BoundTo = binaryOpNode.Op == BinaryOp.Error ? string.Empty : keyword;
                AddSuggestionsForBinaryOperatorKeyWords(intellisenseData);

                return true;
            }

            internal static void AddSuggestionsForBinaryOperatorKeyWords(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                // TASK: 76039: Intellisense: Update intellisense to filter suggestions based on the expected type of the text being typed in UI
                IntellisenseHelper.AddSuggestionsForMatches(intellisenseData, TexlLexer.LocalizedInstance.GetBinaryOperatorKeywords(), SuggestionKind.BinaryOperator, SuggestionIconKind.Other, requiresSuggestionEscaping: false);
            }
        }
    }
}
