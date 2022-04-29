// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal partial class Intellisense
    {
        internal abstract class NodeKindSuggestionHandler : ISuggestionHandler
        {
            private readonly NodeKind _kind;

            public NodeKindSuggestionHandler(NodeKind kind)
            {
                _kind = kind;
            }

            /// <summary>
            /// Adds suggestions as appropriate to the internal Suggestions and SubstringSuggestions lists of intellisenseData.
            /// Returns true if intellisenseData is handled and no more suggestions are to be found and false otherwise.
            /// </summary>
            public bool Run(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);
                Contracts.AssertValue(intellisenseData.CurNode);

                if (intellisenseData.CurNode.Kind != _kind)
                {
                    return false;
                }

                return TryAddSuggestionsForNodeKind(intellisenseData);
            }

            internal abstract bool TryAddSuggestionsForNodeKind(IntellisenseData.IntellisenseData intellisenseData);
        }
    }
}
