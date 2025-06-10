// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Wraps ConnectorSuggestions to add ConnectorType for enhanced suggestions.
    /// </summary>
    public class ConnectorEnhancedSuggestions
    {
        /// <summary>
        /// Gets the connector type associated with the suggestions.
        /// </summary>
        public ConnectorType ConnectorType { get; }

        /// <summary>
        /// Gets the connector suggestions.
        /// </summary>
        public ConnectorSuggestions ConnectorSuggestions { get; }

        internal ConnectorEnhancedSuggestions(SuggestionMethod suggestionMethod, IReadOnlyList<ConnectorSuggestion> suggestions, ConnectorType connectorType = null)
        {
            ConnectorType = connectorType;
            ConnectorSuggestions = new ConnectorSuggestions(suggestionMethod, suggestions, connectorType?.FormulaType);
        }
    }
}
