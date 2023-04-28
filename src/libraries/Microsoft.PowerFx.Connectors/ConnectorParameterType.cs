// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorParameterType
    {
        public FormulaType Type { get; }

        public RecordType HiddenRecordType { get; }

        public ConnectorType ConnectorType { get; }

        public ConnectorType HiddenConnectorType { get; }

        public ConnectorParameterType(OpenApiSchema schema, FormulaType type, RecordType hiddenRecordType)
            : this(type)
        {
            HiddenRecordType = hiddenRecordType;
            ConnectorType = new ConnectorType(schema, type);
        }

        public ConnectorParameterType(OpenApiSchema schema, FormulaType type)
            : this(schema, type, null)
        {
        }

        private ConnectorParameterType(FormulaType type)
        {
            Type = type;
        }

        public ConnectorParameterType()
        {
            Type = FormulaType.Blank;
        }

        public ConnectorParameterType(OpenApiSchema schema, TableType tableType, ConnectorType tableConnectorType)
            : this(tableType)
        {
            HiddenRecordType = null;
            HiddenConnectorType = null;
            ConnectorType = new ConnectorType(schema, tableType, tableConnectorType);
        }

        public ConnectorParameterType(OpenApiSchema schema, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields)
            : this(recordType)
        {
            HiddenRecordType = hiddenRecordType;
            ConnectorType = new ConnectorType(schema, recordType, fields);
            HiddenConnectorType = new ConnectorType(schema, hiddenRecordType, hiddenFields);
        }

        public void SetProperties(string name, bool isRequired)
        {
            ConnectorType.Name = name;
            ConnectorType.IsRequired = isRequired;

            if (HiddenConnectorType != null)
            {
                HiddenConnectorType.Name = name;
                HiddenConnectorType.IsRequired = isRequired;
            }
        }
    }
}
