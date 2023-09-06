// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorParameter : ConnectorSchema
    {
        public string Name { get; internal set; }

        public FormulaValue DefaultValue { get; }

        public string Description { get; }

        internal ConnectorParameter(string name, string description, OpenApiSchema schema, FormulaType type, ConnectorType connectorType, string summary, ConnectorDynamicValue dynamicValue, ConnectorDynamicList dynamicList, ConnectorDynamicSchema dynamicSchema, ConnectorDynamicProperty dynamicProperty, bool numberIsFloat)
            : base(schema, type, connectorType, summary, dynamicValue, dynamicList, dynamicSchema, dynamicProperty)
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
        public IReadOnlyList<ConnectorSuggestion> Suggestions { get; internal set; }

        public FormulaValue Value { get; private set; }

        public FormulaValue[] Values { get; private set; }

        public string[] ParameterNames { get; internal set; }

        internal ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue value)
            : base(connectorParameter)
        {
            Suggestions = new List<ConnectorSuggestion>();
            Value = value;
            Values = null;
        }

        internal ConnectorParameterWithSuggestions(ConnectorParameter connectorParameter, FormulaValue[] values)
            : base(connectorParameter)
        {
            Suggestions = new List<ConnectorSuggestion>();
            Value = null;
            Values = values.ToArray();
        }
    }

    public class ConnectorParameters
    {
        public bool IsCompleted { get; internal set; }

        public ConnectorParameterWithSuggestions[] Parameters { get; internal set; }
    }
}
