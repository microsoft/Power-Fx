// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Wrapper class around FormulaType and ConnectorType
    // FormulaType is used to represent the type of the parameter in the Power Fx expression as used in Power Apps
    // ConnectorType contains more details information coming from the swagger file and extensions
    [DebuggerDisplay("{Type._type}")]
    public class ConnectorParameterType
    {
        public FormulaType FormulaType => ConnectorType.FormulaType;

        // $$$ lucgen
        internal ConnectorType ConnectorType { get; }

        internal RecordType HiddenRecordType { get; }

        internal ConnectorType HiddenConnectorType { get; }

        public bool SupportsSuggestions => DynamicReturnSchema != null || DynamicReturnProperty != null;

        internal ConnectorDynamicSchema DynamicReturnSchema { get; private set; }

        internal ConnectorDynamicProperty DynamicReturnProperty { get; private set; }

        internal ConnectorParameterType(OpenApiSchema schema, string name, bool required, string visibility, FormulaType type, RecordType hiddenRecordType)
        {
            HiddenRecordType = hiddenRecordType;
            ConnectorType = new ConnectorType(schema, name, required, visibility, type);
        }

        internal ConnectorParameterType(OpenApiSchema schema, string name, bool required, string visibility, FormulaType type)
            : this(schema, name, required, visibility, type, null)
        {
        }

        internal ConnectorParameterType()
        {
            ConnectorType = new ConnectorType(null, null, false, null, new BlankType());
        }

        internal ConnectorParameterType(OpenApiSchema schema, string name, bool required, string visibility, TableType tableType, ConnectorType tableConnectorType)
        {
            HiddenRecordType = null;
            HiddenConnectorType = null;
            ConnectorType = new ConnectorType(schema, name, required, visibility, tableType, tableConnectorType);
        }

        internal ConnectorParameterType(OpenApiSchema schema, string name, bool required, string visibility, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields)
        {
            HiddenRecordType = hiddenRecordType;
            ConnectorType = new ConnectorType(schema, name, required, visibility, recordType, fields);
            HiddenConnectorType = new ConnectorType(schema, name, required, visibility, hiddenRecordType, hiddenFields);
        }

        internal void SetDynamicReturnSchemaAndProperty(ConnectorDynamicSchema dynamicSchema, ConnectorDynamicProperty dynamicProperty)
        {
            DynamicReturnSchema = dynamicSchema;
            DynamicReturnProperty = dynamicProperty;
        }
    }
}
