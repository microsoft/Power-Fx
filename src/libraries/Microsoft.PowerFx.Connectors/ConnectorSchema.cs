// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSchema
    {
        public ConnectorParameterType ConnectorParameterType { get; }

        public FormulaValue DefaultValue { get; }

        internal OpenApiSchema Schema { get; }

        internal ConnectorExtensions ConnectorExtensions { get; }

        private bool UseHiddenTypes { get; }

        public string Title => Schema.Title;

        public FormulaType FormulaType => UseHiddenTypes ? ConnectorParameterType.HiddenRecordType : ConnectorParameterType.FormulaType;

        // $$$ lucgen
        internal ConnectorType ConnectorType => UseHiddenTypes ? ConnectorParameterType.HiddenConnectorType : ConnectorParameterType.ConnectorType;

        internal RecordType HiddenRecordType => ConnectorParameterType.HiddenRecordType;

        internal ConnectorType HiddenConnectorType => ConnectorParameterType.HiddenConnectorType;

        /// <summary>
        /// "x-ms-dynamic-values".
        /// </summary>
        internal ConnectorDynamicValue DynamicValue => ConnectorExtensions.ConnectorDynamicValue;

        /// <summary>
        /// "x-ms-dynamic-list".
        /// </summary>
        internal ConnectorDynamicList DynamicList => ConnectorExtensions.ConnectorDynamicList;

        /// <summary>
        /// "x-ms-dynamic-schema".
        /// </summary>
        internal ConnectorDynamicSchema DynamicSchema => ConnectorExtensions.ConnectorDynamicSchema;

        /// <summary>
        /// "x-ms-dynamic-properties".
        /// </summary>
        internal ConnectorDynamicProperty DynamicProperty => ConnectorExtensions.ConnectorDynamicProperty;

        public string Summary => ConnectorExtensions.Summary;

        public bool SupportsDynamicValuesOrList => DynamicValue != null || DynamicList != null;

        public bool SupportsDynamicSchemaOrProperty => DynamicSchema != null || DynamicProperty != null;

        public bool SupportsDynamicIntellisense => SupportsDynamicValuesOrList || SupportsDynamicSchemaOrProperty;

        internal ConnectorSchema(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes, bool numberIsFloat)
        {
            Schema = openApiParameter.Schema;
            UseHiddenTypes = useHiddenTypes;
            ConnectorParameterType = openApiParameter.Schema.ToConnectorParameterType(openApiParameter.Name, openApiParameter.Required, openApiParameter.GetVisibility(), numberIsFloat: numberIsFloat);
            DefaultValue = openApiParameter.Schema.TryGetDefaultValue(FormulaType, out FormulaValue defaultValue, numberIsFloat: numberIsFloat) ? defaultValue : null;
            ConnectorExtensions = new ConnectorExtensions(openApiParameter, bodyExtensions, numberIsFloat);
        }

        internal ConnectorSchema(ConnectorSchema csi, ConnectorParameterType cpt)
        {
            Schema = csi.Schema;
            DefaultValue = csi.DefaultValue;
            ConnectorParameterType = cpt ?? csi.ConnectorParameterType;
            ConnectorExtensions = csi.ConnectorExtensions;
        }
    }
}
