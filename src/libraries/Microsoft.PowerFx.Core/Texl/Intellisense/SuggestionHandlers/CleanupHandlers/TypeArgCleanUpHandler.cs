// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Intellisense.IntellisenseData;

namespace Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionHandlers.CleanupHandlers
{
    internal sealed class TypeArgCleanUpHandler : ISpecialCaseHandler
    {
        public TypeArgCleanUpHandler()
        {
        }

        public bool Run(IIntellisenseContext context, IntellisenseData intellisenseData, List<IntellisenseSuggestion> suggestions)
        {
            Contracts.AssertValue(context);
            Contracts.AssertValue(suggestions);
            Contracts.AssertValue(intellisenseData.CurFunc);

            var removedAny = false;

            if (!intellisenseData.CurFunc.ArgIsType(intellisenseData.ArgIndex))
            {
                return removedAny;
            }

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
