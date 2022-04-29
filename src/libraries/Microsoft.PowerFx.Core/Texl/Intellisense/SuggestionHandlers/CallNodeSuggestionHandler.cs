// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class CallNodeSuggestionHandler : NodeKindSuggestionHandler
        {
            public CallNodeSuggestionHandler()
                : base(NodeKind.Call)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                var curNode = intellisenseData.CurNode;
                var cursorPos = intellisenseData.CursorPos;

                var callNode = curNode.CastCall();
                var spanMin = callNode.Head.Token.Span.Min;
                var spanLim = callNode.Head.Token.Span.Lim;

                // Handling the special case for service functions with non-empty namespaces.
                // We have to consider the namespace as the begining of the callNode for intellisense purposes.
                if (callNode.HeadNode != null)
                {
                    var dottedNode = callNode.HeadNode.AsDottedName();
                    spanMin = dottedNode.Left.Token.Span.Min;
                    spanLim = dottedNode.Right.Token.Span.Lim;
                }

                if (cursorPos < spanMin)
                {
                    // Cursor is before the head
                    // i.e. "| Filter(....)"
                    // Suggest possibilities that can result in a value.
                    IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, callNode);
                }
                else if (cursorPos <= spanLim)
                {
                    // Cursor is in the head.
                    // Suggest function names.
                    // Get the matching string as a substring from the script so that the whitespace is preserved.
                    var replacementLength = IntellisenseHelper.GetReplacementLength(intellisenseData, spanMin, spanLim, intellisenseData.Binding.NameResolver.Functions.Select(function => function.Name));

                    // If we are replacing the full token, also include the opening paren (since this will be provided by the suggestion)
                    if (replacementLength == spanLim - spanMin)
                    {
                        replacementLength += TexlLexer.PunctuatorParenOpen.Length;
                    }

                    intellisenseData.SetMatchArea(spanMin, cursorPos, replacementLength);
                    intellisenseData.BoundTo = intellisenseData.Binding.ErrorContainer.HasErrors(callNode) ? string.Empty : callNode.Head.Name;
                    IntellisenseHelper.AddSuggestionsForFunctions(intellisenseData);
                }
                else if (callNode.Token.Span.Lim > cursorPos || callNode.ParenClose == null)
                {
                    // Handling the erroneous case when user enters a space after functionName and cursor is after space.
                    // Cursor is before the open paren of the function.
                    // Eg: "Filter | (" AND "Filter | (some Table, some predicate)"
                    return false;
                }
                else
                {
                    // If there was no closed parenthesis we would have an error node.
                    Contracts.Assert(callNode.ParenClose != null);

                    if (cursorPos <= callNode.ParenClose.Span.Min)
                    {
                        // Cursor position is before the closed parenthesis and there are no arguments.
                        // If there were arguments FindNode should have returned one of those.
                        if (intellisenseData.CurFunc != null && intellisenseData.CurFunc.MaxArity > 0)
                        {
                            IntellisenseHelper.AddSuggestionsForTopLevel(intellisenseData, callNode);
                        }
                    }
                    else if (IntellisenseHelper.CanSuggestAfterValue(cursorPos, intellisenseData.Script))
                    {
                        // Verify that cursor is after a space after the closed parenthesis and
                        // suggest binary operators.
                        IntellisenseHelper.AddSuggestionsForAfterValue(intellisenseData, intellisenseData.Binding.GetType(callNode));
                    }
                }

                return true;
            }
        }
    }
}