// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a set of connector parameters.
    /// </summary>
    public class ConnectorParameters
    {
        /// <summary>
        /// Indicates that all parameters are having a defined value and we can generate/execute an expression with this parameter set.
        /// </summary>
        public bool IsCompleted { get; internal set; }

        /// <summary>
        /// Gets the parameters with suggestions.
        /// </summary>
        public ConnectorParameterWithSuggestions[] ParametersWithSuggestions { get; internal set; }
    }
}
