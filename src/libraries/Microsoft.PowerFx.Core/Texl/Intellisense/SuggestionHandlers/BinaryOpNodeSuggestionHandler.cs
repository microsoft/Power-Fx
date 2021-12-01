﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class BinaryOpNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public BinaryOpNodeSuggestionHandler()
                : base(NodeKind.BinaryOp)
            { }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                TexlNode curNode = intellisenseData.CurNode;
                // Cursor is in the operation token.
                // Suggest binary operators.
                BinaryOpNode binaryOpNode = curNode.CastBinaryOp();
                var tokenSpan = binaryOpNode.Token.Span;

                string keyword = binaryOpNode.Op == BinaryOp.Error ? tokenSpan.GetFragment(intellisenseData.Script) : TexlParser.GetTokString(binaryOpNode.Token.Kind);
                int replacementLength = tokenSpan.Min == intellisenseData.CursorPos ? 0 : tokenSpan.Lim - tokenSpan.Min;
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