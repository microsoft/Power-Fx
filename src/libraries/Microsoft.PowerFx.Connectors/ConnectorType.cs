// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    [DebuggerDisplay("{Name}: {Description}")]
    public class ConnectorType
    {
        // "name"
        public string Name { get; internal set; }

        // "x-ms-summary"
        public string DisplayName { get; }

        // "description"
        public string Description { get; }

        // "required"
        public bool IsRequired { get; internal set; }        

        // Only used for RecordType and TableType
        public ConnectorType[] Fields { get; }

        // FormulaType
        public FormulaType FormulaType { get; }

        public ConnectorType(OpenApiSchema schema, FormulaType formulaType)            
        {            
            FormulaType = formulaType;
            Description = schema.Description;
            DisplayName = ArgumentMapper.GetSummary(schema);
            Fields = Array.Empty<ConnectorType>();
            IsRequired = false;
            Name = null;
        }

        public ConnectorType(OpenApiSchema schema, RecordType recordType, ConnectorType[] fields)
            : this(schema, recordType)
        {            
            Fields = fields;
        }

        public ConnectorType(OpenApiSchema schema, TableType recordType, ConnectorType field)
            : this(schema, recordType)
        {            
            Fields = new ConnectorType[] { field };
        }
    }
}
