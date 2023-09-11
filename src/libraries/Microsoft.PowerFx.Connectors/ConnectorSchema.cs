// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSchema
    {
        public OpenApiSchema Schema { get; init; }

        public FormulaType FormulaType { get; init; }

        public ConnectorType ConnectorType { get; init; }

        /// <summary>
        /// "x-ms-dynamic-values".
        /// </summary>
        internal ConnectorDynamicValue DynamicValue { get; init; }

        /// <summary>
        /// "x-ms-dynamic-list".
        /// </summary>
        internal ConnectorDynamicList DynamicList { get; init; }

        /// <summary>
        /// "x-ms-dynamic-schema".
        /// </summary>
        internal ConnectorDynamicSchema DynamicSchema { get; init; }

        /// <summary>
        /// "x-ms-dynamic-properties".
        /// </summary>
        internal ConnectorDynamicProperty DynamicProperty { get; init; }

        public string Summary { get; init; }

        public bool SupportsDynamicValuesOrList => DynamicValue != null || DynamicList != null;

        public bool SupportsDynamicSchemaOrProperty => DynamicSchema != null || DynamicProperty != null;

        public bool SupportsDynamicIntellisense => SupportsDynamicValuesOrList || SupportsDynamicSchemaOrProperty;

        internal ConnectorSchema(OpenApiSchema schema, FormulaType type, ConnectorType connectorType, string summary, ConnectorDynamicValue dynamicValue, ConnectorDynamicList dynamicList, ConnectorDynamicSchema dynamicSchema, ConnectorDynamicProperty dynamicProperty)
        {
            Schema = schema;
            FormulaType = type;
            ConnectorType = connectorType;
            Summary = summary;
            DynamicValue = dynamicValue;
            DynamicList = dynamicList;
            DynamicSchema = dynamicSchema;
            DynamicProperty = dynamicProperty;
        }

        internal ConnectorSchema(ConnectorSchema csi)
        {
            Schema = csi.Schema;
            FormulaType = csi.FormulaType;
            ConnectorType = csi.ConnectorType;
            Summary = csi.Summary;
            DynamicValue = csi.DynamicValue;
            DynamicList = csi.DynamicList;
            DynamicSchema = csi.DynamicSchema;
            DynamicProperty = csi.DynamicProperty;
        }
    }
}
