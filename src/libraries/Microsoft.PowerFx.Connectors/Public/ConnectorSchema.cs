// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSchema
    {
        public ConnectorType ConnectorType { get; }

        public FormulaValue DefaultValue { get; }

        internal OpenApiSchema Schema { get; }

        internal ConnectorExtensions ConnectorExtensions { get; }

        private bool UseHiddenTypes { get; }

        public string Title => Schema.Title;

        public FormulaType FormulaType => UseHiddenTypes ? ConnectorType.HiddenRecordType : ConnectorType.FormulaType;
       
        internal RecordType HiddenRecordType => ConnectorType.HiddenRecordType;

        public string Summary => ConnectorExtensions.Summary;

        public bool SupportsDynamicIntellisense => ConnectorType.SupportsDynamicIntellisense;

        internal ConnectorSchema(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes)
        {
            Schema = openApiParameter.Schema;
            UseHiddenTypes = useHiddenTypes;
            ConnectorType = openApiParameter.ToConnectorType();
            DefaultValue = openApiParameter.Schema.TryGetDefaultValue(FormulaType, out FormulaValue defaultValue) ? defaultValue : null;

            ConnectorExtensions = new ConnectorExtensions(openApiParameter, bodyExtensions);
        }

        internal ConnectorSchema(ConnectorSchema connectorSchema, ConnectorType connectorType)
        {
            Schema = connectorSchema.Schema;
            DefaultValue = connectorSchema.DefaultValue;
            ConnectorType = connectorType ?? connectorSchema.ConnectorType;
            ConnectorExtensions = connectorSchema.ConnectorExtensions;
        }
    }
}
