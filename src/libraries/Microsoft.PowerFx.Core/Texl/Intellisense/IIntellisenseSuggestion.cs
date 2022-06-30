// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerFx.Intellisense
{
    public interface IIntellisenseSuggestion
    {
        /// <summary>
        /// The Kind of Suggestion.
        /// </summary>
        SuggestionKind Kind { get; }

        /// <summary>
        /// What kind of icon to display next to the suggestion.
        /// </summary>
        SuggestionIconKind IconKind { get; }

        /// <summary>
        /// This is the string that will be displayed to the user.
        /// </summary>
        UIString DisplayText { get; }

        /// <summary>
        /// Indicates if there are errors.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Description, suitable for UI consumption.
        /// </summary>
        string FunctionParameterDescription { get; }

        /// <summary>
        /// Description, suitable for UI consumption.
        /// </summary>
        string Definition { get; }

        /// <summary>
        /// A boolean value indicating if the suggestion matches the expected type in the rule.
        /// </summary>
        bool IsTypeMatch { get; }

        /// <summary>
        /// Returns the list of suggestions for the overload of the function.
        /// This is populated only if the suggestion kind is a function and if the function has overloads.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "n/a")]
        IEnumerable<IIntellisenseSuggestion> Overloads { get; }

        /// <summary>
        /// A boolean value indicating if the suggestion should be preselected by the formula bar
        /// In canvas, used for Primary Output properties.
        /// </summary>
        bool ShouldPreselect { get; }
    }
}
