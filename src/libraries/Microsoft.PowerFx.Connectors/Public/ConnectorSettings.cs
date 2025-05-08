// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Settings for a connector.
    /// </summary>
    [ThreadSafeImmutable]
    public class ConnectorSettings
    {
        public static ConnectorSettings NewCDPConnectorSettings(bool extractSensitivityLabel = false, string purviewAccountName = null)
        {
            var connectorSettings = new ConnectorSettings(null)
            {
                Compatibility = ConnectorCompatibility.CdpCompatibility,
                SupportXMsEnumValues = true,
                ReturnEnumsAsPrimitive = false,
                ExtractSensitivityLabel = extractSensitivityLabel,
                PurviewAccountName = purviewAccountName
            };

            return connectorSettings;
        }
        
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
        /// If this is enabled it will extract MIP Labels.
        /// </summary>
        internal bool ExtractSensitivityLabel { get; init; } = false;

        internal string PurviewAccountName { get; init; }

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
        /// Enables the suggestion mapping logic to use "value" and "displayName" as fallback property names.
        /// Serves as a safeguard when the actual response from the suggestion API doesn't align with the Swagger specification.
        /// </summary>
        public bool AllowSuggestionMappingFallback { get; init; } = false;

        /// <summary>
        /// Include webhook functions that contain "x-ms-notification-content" in definition.
        /// By default these functions won't be accessible by end users.
        /// </summary>
        public bool IncludeWebhookFunctions { get; init; } = false;

        /// <summary>        
        /// By default these functions won't be accessible by end users.
        /// Internally, internal functions will be kept (ConnectorFunction.FunctionList) as some of those are used for dynamic intellisense.
        /// </summary>
        public bool IncludeInternalFunctions { get; init; } = false;

        /// <summary>
        /// By default, internal parameters without default values are ignored, mandatory or not.
        /// With this setting turned on, mandatory internal parameters will be exposed.
        /// When Compatibility is set to PowerAppsCompabiliity, this parameter is always true.
        /// </summary>
        public bool ExposeInternalParamsWithoutDefaultValue
        {
            get => _exposeInternalParamsWithoutDefaultValue || Compatibility == ConnectorCompatibility.PowerAppsCompatibility;
            init => _exposeInternalParamsWithoutDefaultValue = value;
        }

        private bool _exposeInternalParamsWithoutDefaultValue = false;

        /// <summary>
        /// In Power Apps, all record fields which are not declared in the swagger file will not be part of the Power Fx response.
        /// ReturnUnknownRecordFieldsAsUntypedObjects modifies this behavior to return all unknown fields as UntypedObjects. 
        /// This flag is only working when Compatibility is set to ConnectorCompatibility.SwaggerCompatibility or ConnectorCompatibility.CdpCompatibility.
        /// </summary>
        public bool ReturnUnknownRecordFieldsAsUntypedObjects { get; init; } = false;

        /// <summary>
        /// By default action connectors won't parse x-ms-enum-values.
        /// Only CDP connectors will have this enabled by default.
        /// </summary>
        public bool SupportXMsEnumValues { get; init; } = false;

        /// <summary>
        /// This flag will force all enums to be returns as FormulaType.String or FormulaType.Decimal regardless of x-ms-enum-*.
        /// This flag is only in effect when SupportXMsEnumValues is true.
        /// </summary>
        public bool ReturnEnumsAsPrimitive { get; init; } = false;

        /// <summary>
        /// This flag enables some special handling for the body parameter, when
        /// - body name is 'item'
        /// - body inner object is 'dynamicProperties'
        /// - there is only one property in inner object
        /// In that base the body will be fully flattened and we will retain the 'body' name for the parameter.
        /// </summary>
        public bool UseItemDynamicPropertiesSpecialHandling { get; init; } = false;
        
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
        SwaggerCompatibility = 2,

        // Swagger File Conformity for CDP connectors
        // - same as Swagger File Conformity
        // - does not require format="enum" for identifying enumerations
        CdpCompatibility = 3
    }
}
