// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Represents a collection of connector suggestions for PowerFx intellisense.
    /// </summary>
    public class ConnectorSuggestions
    {
        /// <summary>
        /// Gets the list of connector suggestions.
        /// </summary>
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; }
        
        /// <summary>
        /// Gets the formula type associated with the suggestions.
        /// </summary>
        public FormulaType FormulaType { get; }

        internal SuggestionMethod SuggestionMethod;

        internal ConnectorSuggestions(SuggestionMethod suggestionMethod, IReadOnlyList<ConnectorSuggestion> suggestions, FormulaType formulaType = null)
        {
            SuggestionMethod = suggestionMethod;
            Suggestions = suggestions;
            FormulaType = formulaType;
        }
    }

    /// <summary>
    /// Represents a single connector suggestion for PowerFx intellisense.
    /// </summary>
    public class ConnectorSuggestion
    {
        /// <summary>
        /// Gets the formula value for the suggestion.
        /// </summary>
        public FormulaValue Suggestion { get; }

        /// <summary>
        /// Gets the display name for the suggestion.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorSuggestion"/> class.
        /// </summary>
        /// <param name="suggestion">The formula value for the suggestion.</param>
        /// <param name="displayName">The display name for the suggestion.</param>
        public ConnectorSuggestion(FormulaValue suggestion, string displayName)
        {
            Suggestion = suggestion;
            DisplayName = displayName;
        }
    }

    internal enum SuggestionMethod
    {
        None = 0,
        DynamicList = 1,
        DynamicValue = 2,
        DynamicProperty = 3,
        DynamicSchema = 4
    }
}
