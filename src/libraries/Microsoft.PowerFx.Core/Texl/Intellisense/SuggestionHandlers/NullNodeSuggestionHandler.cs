// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal partial class Intellisense
    {
        internal sealed class NullNodeSuggestionHandler : ISuggestionHandler
        {
            public NullNodeSuggestionHandler()
            {
            }

            /// <summary>
            /// Adds suggestions as appropriate to the internal Suggestions and SubstringSuggestions lists of intellisenseData.
            /// Returns true if intellisenseData is handled and no more suggestions are to be found and false otherwise.
            /// </summary>
            public bool Run(IntellisenseData.IntellisenseData intellisenseData)
            {
                Contracts.AssertValue(intellisenseData);

                if (intellisenseData.CurNode != null)
                {
                    return false;
                }

                return IntellisenseHelper.AddSuggestionsForValuePossibilities(intellisenseData, null);
            }
        }
    }
}