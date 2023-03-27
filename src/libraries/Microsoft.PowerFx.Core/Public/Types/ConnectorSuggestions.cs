// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Types
{
    public class ConnectorSuggestions
    {
        public List<ConnectorSuggestion> Suggestions { get; internal set; }

        public ErrorValue Error { get; }

        public ConnectorSuggestions(ErrorValue error) 
        {
            Suggestions = null;
            Error = error;
        }

        public ConnectorSuggestions(List<ConnectorSuggestion> suggestions)
        {
            Suggestions = suggestions;
            Error = null;
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
}
