// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a parameter of a connector function.
    /// </summary>
    public class ConnectorParameter : ConnectorSchema
    {
        public string Name { get; internal set; }

        public FormulaValue DefaultValue { get; }

        public string Description { get; }

        internal ConnectorParameter(string name, string description, OpenApiSchema schema, FormulaType type, ConnectorType connectorType, ConnectorExtensions extensions, bool numberIsFloat)           
            : base(schema, type, connectorType, extensions)
        {
            Name = name;
            Description = description;
            DefaultValue = schema.TryGetDefaultValue(type, out FormulaValue dv, numberIsFloat: numberIsFloat) ? dv : null;
        }

        internal ConnectorParameter(ConnectorParameter cpi)
            : base(cpi)
        {
            Name = cpi.Name;
            DefaultValue = cpi.DefaultValue;
            Description = cpi.Description;
        }
    }

    public class ConnectorParameterWithSuggestions : ConnectorParameter
    {
        /// <summary>
        /// List of suggestions.
        /// </summary>
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; }
        
        public ConnectorParameterType ConnectorParameterType { get; }

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
            : base(connectorParameter)
        {
            Suggestions = new List<ConnectorSuggestion>();         
            Value = value;
            Values = null;
            ConnectorParameterType = null;

            bool dynamicPropertyOrSchema = (suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicProperty || suggestions?.ConnectorSuggestions.SuggestionMethod == SuggestionMethod.DynamicSchema) == true;
            bool error = suggestions == null;
            if (!error && connectorParameter.Name == parameterName)
            {
                if (dynamicPropertyOrSchema)
                {
                    Suggestions = suggestions.ConnectorSuggestions.Suggestions.Where(s => !knownParameters.Any(kp => kp.Name == s.DisplayName)).ToList();
                    Values = suggestions.ConnectorSuggestions.Suggestions.Join(knownParameters, (ConnectorSuggestion cs) => cs.DisplayName, (NamedValue nv) => nv.Name, (ConnectorSuggestion cs, NamedValue nv) => nv.Value).ToArray();
                    ParameterNames = suggestions.ConnectorSuggestions.Suggestions.Select(s => s.DisplayName).ToArray();
                    FormulaType = suggestions.ConnectorSuggestions.FormulaType;
                    ConnectorParameterType = suggestions.ConnectorParameterType;
                }
                else
                {
                    Suggestions = suggestions.ConnectorSuggestions.Suggestions;
                }   
            }
        }
    }

    public class ConnectorParameters
    {
        /// <summary>
        /// Indicates that all parameters are having a defined value and we can generate/execute an expression with this parameter set.
        /// </summary>
        public bool IsCompleted { get; internal set; }

        public ConnectorParameterWithSuggestions[] ParametersWithSuggestions { get; internal set; }
    }
}
