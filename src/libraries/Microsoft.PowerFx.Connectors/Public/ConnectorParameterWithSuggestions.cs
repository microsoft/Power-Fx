// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorParameterWithSuggestions : ConnectorParameter
    {
        /// <summary>
        /// List of suggestions.
        /// </summary>
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; }

        /// <summary>
        /// Parameter value.
        /// </summary>
        public FormulaValue Value { get; }

        /// <summary>
        /// Parameter values for optional parameters.
        /// </summary>
        public FormulaValue[] Values { get; }

        /// <summary>
        /// List of parameter names for optional parameters.
        /// </summary>
        public string[] ParameterNames { get; }

        internal ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue value, string parameterName, ConnectorEnhancedSuggestions suggestions, NamedValue[] knownParameters)
            : base(connectorParameter, GetConnectorType(connectorParameter, parameterName, suggestions))
        {
            Suggestions = new List<ConnectorSuggestion>();
            Value = value;
            Values = null;

            bool dynamicPropertyOrSchema = (suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicProperty || suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicSchema) == true;
            bool error = suggestions == null;
            if (!error && connectorParameter.Name == parameterName)
            {
                if (dynamicPropertyOrSchema)
                {
                    Suggestions = suggestions.ConnectorSuggestions.Suggestions.Where(s => !knownParameters.Any(kp => kp.Name == s.DisplayName)).ToList();
                    Values = suggestions.ConnectorSuggestions.Suggestions.Join(knownParameters, (ConnectorSuggestion cs) => cs.DisplayName, (NamedValue nv) => nv.Name, (ConnectorSuggestion cs, NamedValue nv) => nv.Value).ToArray();
                    ParameterNames = suggestions.ConnectorSuggestions.Suggestions.Select(s => s.DisplayName).ToArray();
                }
                else
                {
                    Suggestions = suggestions.ConnectorSuggestions.Suggestions;
                }
            }
        }

        private static ConnectorType GetConnectorType(ConnectorParameter connectorParameter, string parameterName, ConnectorEnhancedSuggestions suggestions)
        {
            bool dynamicPropertyOrSchema = (suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicProperty || suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicSchema) == true;
            return suggestions != null && connectorParameter.Name == parameterName && dynamicPropertyOrSchema ? suggestions?.ConnectorType : null;
        }
    }
}
