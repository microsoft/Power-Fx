// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Connectors
{
    // Wraps ConnectorSuggestions (defined in PFx.Core) to add ConnectorType (Pfx.Connectors only)
    public class ConnectorEnhancedSuggestions
    {
        public ConnectorType ConnectorType { get; }

        internal ConnectorSuggestions ConnectorSuggestions { get; }

        internal ConnectorEnhancedSuggestions(SuggestionMethod suggestionMethod, IReadOnlyList<ConnectorSuggestion> suggestions, ConnectorType connectorType = null)
        {
            ConnectorType = connectorType;
            ConnectorSuggestions = new ConnectorSuggestions(suggestionMethod, suggestions, connectorType?.FormulaType);
        }
    }
}
