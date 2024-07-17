// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpRecordType : RecordType
    {
        internal ConnectorType ConnectorType { get; }

        internal IList<ReferencedEntity> ReferencedEntities { get; }

        internal ICdpTableResolver TableResolver { get; }

        internal CdpRecordType(ConnectorType connectorType, DType recordType, ICdpTableResolver tableResolver, IList<ReferencedEntity> referencedEntities)
            : base(recordType)
        {
            ConnectorType = connectorType;
            TableResolver = tableResolver;
            ReferencedEntities = referencedEntities;
        }

        public bool TryGetFieldExternalTableName(string fieldName, out string tableName)
        {
            tableName = null;

            if (!base.TryGetBackingDType(fieldName, out _))
            {                
                return false;
            }

            ConnectorType connectorType = ConnectorType.Fields.First(ct => ct.Name == fieldName);

            if (connectorType.ExternalTables?.Any() != true)
            {             
                return false;
            }

            tableName = connectorType.ExternalTables.First();
            return true;
        }

        public override bool TryGetFieldType(string fieldName, out FormulaType type)
        {
            if (!base.TryGetBackingDType(fieldName, out _))
            {
                type = null;
                return false;
            }

            ConnectorType cr = ConnectorType.Fields.First(ct => ct.Name == fieldName);

            if (cr.ExternalTables?.Any() != true)
            {
                return base.TryGetFieldType(fieldName, out type);
            }

            string tableName = cr.ExternalTables.First();

            try
            {
                CdpTableDescriptor ttd = TableResolver.ResolveTableAsync(tableName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                type = ttd.ConnectorType.FormulaType;
                return true;
            }
            catch (Exception ex)
            {
                TableResolver?.Logger.LogException(ex, $"Cannot resolve external table {tableName}");
                throw;
            }
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public override bool Equals(object other)
        {
            throw new NotImplementedException();
        }
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        public override string TableSymbolName => ConnectorType.Name;

        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);
    }
}
