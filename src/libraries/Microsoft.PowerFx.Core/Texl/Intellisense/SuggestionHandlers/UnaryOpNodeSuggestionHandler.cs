// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense{
    internal partial class Intellisense
    {
        internal sealed class UnaryOpNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public UnaryOpNodeSuggestionHandler()
                : base(NodeKind.UnaryOp)
            { }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                TexlNode curNode = intellisenseData.CurNode;
                int cursorPos = intellisenseData.CursorPos;
                // Cursor is in the operation token or before.
                // Suggest all value possibilities.
                UnaryOpNode unaryOpNode = curNode.CastUnaryOp();
                var tokenSpan = unaryOpNode.Token.Span;

                if (cursorPos < tokenSpan.Min)
                    return false;

                Contracts.Assert(cursorPos >= tokenSpan.Min || cursorPos <= tokenSpan.Lim);

                string keyword = TexlParser.GetTokString(unaryOpNode.Token.Kind);
                Contracts.Assert(intellisenseData.MatchingLength <= keyword.Length);

                var replacementLength = tokenSpan.Min == cursorPos ? 0 : tokenSpan.Lim - tokenSpan.Min;
                intellisenseData.SetMatchArea(tokenSpan.Min, cursorPos, replacementLength);
                intellisenseData.BoundTo = intellisenseData.MatchingLength == 0 ? string.Empty : keyword;
                IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, curNode);

                return true;
            }
        }
    }
}