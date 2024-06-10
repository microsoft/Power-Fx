// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorRelationships
    {
        internal ConnectorType _connectorType;

        // Fields that have a relationship with another table
        public IList<ReferencedEntity> FieldsWithRelationship { get; }

        // External tables having a relationship with this table (could be circular)
        public IList<ReferencedEntity> ReferencedEntities { get; }
        
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
