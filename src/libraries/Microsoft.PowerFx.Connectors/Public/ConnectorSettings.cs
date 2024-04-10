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
        /// Unknown extensions in swagger file are ignored by default during the validation process.
        /// </summary>
        public bool FailOnUnknownExtension { get; init; } = false;

        /// <summary>
        /// Allow using functions that are identified as unsupported.
        /// NotSupportedReason property will still be specified.
        /// </summary>
        public bool AllowUnsupportedFunctions { get; init; } = false;

        /// <summary>        
        /// By default these functions won't be accessible by end users.
        /// Internally, internal functions will be kept (ConnectorFunction.FunctionList) as some of those are used for dynamic intellisense.
        /// </summary>
        public bool IncludeInternalFunctions { get; init; } = false;

        /// <summary>
        /// In Power Apps, all record fields which are not declared in the swagger file will not be part of the Power Fx response.
        /// ReturnUnknownRecordFieldsAsUntypedObjects modifies this behavior to return all unknown fields as UntypedObjects. 
        /// This flag is only working when Compatibility is set to ConnectorCompatibility.SwaggerCompatibility.
        /// </summary>
        public bool ReturnUnknownRecordFieldsAsUntypedObjects { get; init; } = false;

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
