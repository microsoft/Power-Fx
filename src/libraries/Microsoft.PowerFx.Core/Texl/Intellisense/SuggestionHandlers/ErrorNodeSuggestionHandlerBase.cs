// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal abstract class ErrorNodeSuggestionHandlerBase : NodeKindSuggestionHandler
        {
            public ErrorNodeSuggestionHandlerBase(NodeKind kind)
                : base(kind)
            {
            }

            internal override bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                // For Error Kind, suggest top level values only in the context of a callNode and
                // ThisItemProperties only in the context of thisItem.
                var curNode = intellisenseData.CurNode;

                if (curNode.Parent.Kind == NodeKind.StrInterp)
                {
                    return true;
                }

                // Three methods that implement custom behavior here, one that adds suggestions before
                // top level suggestions are added, one after, and one to handle the case where there aren't
                // any top level suggestions to add.
                if (intellisenseData.AddSuggestionsBeforeTopLevelErrorNodeSuggestions())
                {
                    return true;
                }

                if (!IntellisenseHelper.AddSuggestionsForTopLevel(intellisenseData, curNode))
                {
                    intellisenseData.AddAlternativeTopLevelSuggestionsForErrorNode();
                }

                intellisenseData.AddSuggestionsAfterTopLevelErrorNodeSuggestions();
                return true;
            }
        }
    }
}
