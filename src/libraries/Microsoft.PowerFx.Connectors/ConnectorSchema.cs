// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSchema
    {
        public OpenApiSchema Schema { get; init; }

        public FormulaType FormulaType { get; internal set; }

        public ConnectorType ConnectorType { get; init; }

        internal ConnectorExtensions ConnectorExtensions { get; }

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

        internal ConnectorSchema(OpenApiSchema schema, FormulaType type, ConnectorType connectorType, ConnectorExtensions extensions)
        {
            Schema = schema;
            FormulaType = type;
            ConnectorType = connectorType;
            ConnectorExtensions = extensions;
        }

        internal ConnectorSchema(ConnectorSchema csi)
        {
            Schema = csi.Schema;
            FormulaType = csi.FormulaType;
            ConnectorType = csi.ConnectorType;
            ConnectorExtensions = csi.ConnectorExtensions;
        }
    }
}
