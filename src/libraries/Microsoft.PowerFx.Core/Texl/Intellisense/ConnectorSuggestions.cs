// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Intellisense
{
    internal class ConnectorSuggestions
    {
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; }
                
        public FormulaType FormulaType { get; }

        internal SuggestionMethod SuggestionMethod;

        internal ConnectorSuggestions(SuggestionMethod suggestionMethod, IReadOnlyList<ConnectorSuggestion> suggestions, FormulaType formulaType = null)
        {
            SuggestionMethod = suggestionMethod;
            Suggestions = suggestions;
            FormulaType = formulaType;
        }
    }

    public class ConnectorSuggestion
    {
        public FormulaValue Suggestion { get; }

        public string DisplayName { get; }

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
