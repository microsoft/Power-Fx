// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        /// NumberIsFloat.
        /// </summary>
        public bool NumberIsFloat { get; init; } = false;

        /// <summary>
        /// Maximum number of rows to return, per page.
        /// </summary>
        public int MaxRows { get; init; } = 1000;

        /// <summary>
        /// Unknown extensions in swagger file will be ignored during the validation process.
        /// </summary>
        public bool IgnoreUnknownExtensions { get; init; } = false;

        /// <summary>
        /// Allow using functions that are identified as unsupported.
        /// NotSupportedReason property will still be specified.
        /// </summary>
        public bool AllowUnsupportedFunctions { get; init; } = false;

        /// <summary>
        /// Throw an exception when an error occurs (HTTP status code >= 300).
        /// </summary>
        public bool ThrowOnError { get; init; } = false;

        /// <summary>
        /// Only used internally for dynamic intellisense.
        /// </summary>
        internal bool ReturnRawResult { get; init; } = false;              
    }
}
