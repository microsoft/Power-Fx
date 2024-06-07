﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Connectors.Tabular;
using static Microsoft.PowerFx.Connectors.ConnectorFunction;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorRelationships
    {
        internal ConnectorType _connectorType;

        // Fields that have a relationship with another table
        public List<ReferencedEntity> FieldsWithRelationship { get; }

        // External tables having a relationship with this table (could be be circular)
        public List<ReferencedEntity> ReferencedEntities { get; }
        
        internal ConnectorRelationships(ConnectorType connectorType)
        {
            _connectorType = connectorType;

            if (connectorType.FormulaType is TabularRecordType trt)
            {
                ReferencedEntities = trt.ReferencedEntities;
            }

            FieldsWithRelationship = new List<ReferencedEntity>();

            foreach (ConnectorType field in connectorType.Fields.Where(ct => ct.ExternalTables != null && ct.ExternalTables.Count == 1))
            {
                FieldsWithRelationship.Add(new ReferencedEntity() { FieldName = field.Name, RelationshipName = field.RelationshipName, TableName = field.ExternalTables[0] });
            }
        }
    }    
}
