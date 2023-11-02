// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Settings for a connector.
    /// </summary>
    public class ConnectorSettings 
    {        
        public ConnectorSettings(string @namespace)
        {
            Namespace = @namespace;
        }

        /// <summary>
        /// Namespace of the connector.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Maximum number of rows to return, per page.
        /// </summary>
        public int MaxRows { get; init; } = 1000;

        /// <summary>
        /// Unknown extensions in swagger file will be ignored by default.
        /// This flag allows to not support unknown extensions when turned to true.
        /// </summary>
        public bool IgnoreUnknownExtensions { get; init; } = false;

        /// <summary>
        /// Allow using functions that are identified as unsupported.
        /// NotSupportedReason property will still be specified.
        /// </summary>
        public bool AllowUnsupportedFunctions { get; init; } = false;    
        
        public ConnectorCompatibility Compatibility { get; init; } = ConnectorCompatibility.Default;
    }

    public enum ConnectorCompatibility
    {
        Default = PowerAppsCompatibility,
        
        // Power Apps Compatibility
        // - required parameters can be reordered based on their locations
        // - required internal visible parameters with defaults are required
        PowerAppsCompatibility = 1,

        // Swagger File Conformity
        // - parameters appear in the order specified in the swagger file
        // - internal visible parameters are completely hidden (required/optional, with or without default value)
        SwaggerCompatibility = 2
    }
}
