// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorEnhancedSuggestions
    {
        public ConnectorParameterType ConnectorParameterType { get; }

        internal ConnectorSuggestions ConnectorSuggestions { get; }

        internal ConnectorEnhancedSuggestions(SuggestionMethod suggestionMethod, IReadOnlyList<ConnectorSuggestion> suggestions, ConnectorParameterType connectorParameterType = null)
        {
            ConnectorParameterType = connectorParameterType;
            ConnectorSuggestions = new ConnectorSuggestions(suggestionMethod, suggestions, connectorParameterType?.Type);
        }
    }
}
