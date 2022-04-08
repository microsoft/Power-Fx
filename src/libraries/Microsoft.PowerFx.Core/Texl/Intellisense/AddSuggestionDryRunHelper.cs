// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    [ThreadSafeImmutable]
    internal class AddSuggestionDryRunHelper : AddSuggestionHelper
    {
        protected override bool CheckAndAddSuggestion(IntellisenseSuggestionList suggestions, IntellisenseSuggestion candidate)
        {
            return !suggestions.ContainsSuggestion(candidate.DisplayText.Text);
        }

        protected override UIString ConstructUIString(SuggestionKind suggestionKind, DType type, IntellisenseSuggestionList suggestions, string valueToSuggest, int highlightStart, int highlightEnd)
        {
            return new UIString(valueToSuggest, highlightStart, highlightEnd);
        }
    }
}
