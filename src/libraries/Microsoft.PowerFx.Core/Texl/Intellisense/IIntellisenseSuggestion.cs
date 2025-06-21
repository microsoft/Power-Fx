// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Represents a suggestion provided by the IntelliSense system, including its type, display text,  and additional
    /// metadata for UI and functionality purposes.
    /// </summary>
    /// <remarks>This interface is used to encapsulate information about a single IntelliSense suggestion, 
    /// such as its kind, display text, and whether it matches the expected type. It also provides  additional details
    /// like overload suggestions and UI-specific metadata.</remarks>
    public interface IIntellisenseSuggestion
    {
        /// <summary>
        /// Gets the Kind of Suggestion.
        /// </summary>
        SuggestionKind Kind { get; }

        /// <summary>
        /// Gets the kind of icon to display next to the suggestion.
        /// </summary>
        SuggestionIconKind IconKind { get; }

        /// <summary>
        /// Gets the string that will be displayed to the user.
        /// </summary>
        UIString DisplayText { get; }

        /// <summary>
        /// Gets whether there are errors.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets the description, suitable for UI consumption.
        /// </summary>
        string FunctionParameterDescription { get; }

        /// <summary>
        /// Gets the description, suitable for UI consumption.
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
        IEnumerable<IIntellisenseSuggestion> Overloads { get; }

        /// <summary>
        /// Gets a value indicating if the suggestion should be preselected by the formula bar
        /// In canvas, used for Primary Output properties.
        /// </summary>
        bool ShouldPreselect { get; }
    }
}
