// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Intellisense.IntellisenseData;

namespace Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionHandlers.CleanupHandlers
{
    // Power Fx Intellisense uses clean-up handlers to filter any unnecessary intellisense data that is added as a part of default logic.
    // For Type arguments, we do not want to suggest suggestions other than SuggestionKind.Type as they are not relevant.
    // This handler is added to Intellisense.CleanupHandlers when only type suggestions are expected in the output.
    internal sealed class TypeArgCleanUpHandler : ISpecialCaseHandler
    {
        public bool Run(IIntellisenseContext context, IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(suggestions);
            Contracts.AssertValue(intellisenseData.CurFunc);
            Contracts.Assert(intellisenseData.CurFunc.ArgIsType(intellisenseData.ArgIndex));

            var removedAny = false;

            var iterateSuggestions = suggestions.ToArray();

            foreach (var suggestion in iterateSuggestions)
            {
                if (suggestion.Kind != SuggestionKind.Type)
                {
                    suggestions.Remove(suggestion);
                    removedAny = true;
                }
            }

            return removedAny;
        }
    }
}
