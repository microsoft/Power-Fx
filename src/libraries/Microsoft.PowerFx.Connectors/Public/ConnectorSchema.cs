// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents the schema of a connector parameter or type.
    /// </summary>
    [DebuggerDisplay("{ConnectorType}")]
    public class ConnectorSchema : SupportsConnectorErrors
    {
        /// <summary>
        /// Gets the connector type.
        /// </summary>
        public ConnectorType ConnectorType { get; }

        /// <summary>
        /// Gets the default value for the parameter or type.
        /// </summary>
        public FormulaValue DefaultValue { get; }

        internal ISwaggerSchema Schema { get; }

        internal ConnectorExtensions ConnectorExtensions { get; }

        private bool UseHiddenTypes { get; }

        /// <summary>
        /// Gets the title of the connector.
        /// </summary>
        public string Title => Schema.Title;

        /// <summary>
        /// Gets the formula type, considering hidden types if applicable.
        /// </summary>
        public FormulaType FormulaType => UseHiddenTypes ? ConnectorType.HiddenRecordType : ConnectorType.FormulaType;

        internal RecordType HiddenRecordType => ConnectorType.HiddenRecordType;

        /// <summary>
        /// Gets the summary of the connector.
        /// </summary>
        public string Summary => ConnectorExtensions.Summary;

        /// <summary>
        /// Indicates whether dynamic intellisense is supported.
        /// </summary>
        public bool SupportsDynamicIntellisense => ConnectorType.SupportsDynamicIntellisense;

        /// <summary>
        /// Gets the notification URL, if available.
        /// </summary>
        public bool? NotificationUrl => ConnectorType.NotificationUrl;

        /// <summary>
        /// Gets the AI sensitivity level.
        /// </summary>
        public AiSensitivity AiSensitivity => ConnectorType.AiSensitivity;

        /// <summary>
        /// Gets the property entity type.
        /// </summary>
        public string PropertyEntityType => ConnectorType.PropertyEntityType;

        internal ConnectorSchema(ISwaggerParameter openApiParameter, ISwaggerExtensions bodyExtensions, bool useHiddenTypes, ConnectorSettings settings)
        {
            Schema = openApiParameter.Schema;
            UseHiddenTypes = useHiddenTypes;
            ConnectorType = AggregateErrorsAndWarnings(openApiParameter.GetConnectorType(settings));
            DefaultValue = openApiParameter.Schema.TryGetDefaultValue(FormulaType, out FormulaValue defaultValue, this) && defaultValue is not BlankValue ? defaultValue : null;
            ConnectorExtensions = new ConnectorExtensions(openApiParameter, bodyExtensions);
        }

        // Intellisense only
        internal ConnectorSchema(ConnectorSchema connectorSchema, ConnectorType connectorType)
        {
            Schema = connectorSchema.Schema;
            DefaultValue = connectorSchema.DefaultValue;
            ConnectorType = AggregateErrorsAndWarnings(connectorType ?? connectorSchema.ConnectorType);
            ConnectorExtensions = connectorSchema.ConnectorExtensions;
            AggregateErrorsAndWarnings(connectorSchema);
            AggregateErrorsAndWarnings(connectorType);
        }
    }
}
