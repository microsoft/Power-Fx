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
    public class TabularRecordType : RecordType
    {
        internal ConnectorType ConnectorType { get; }

        internal IList<ReferencedEntity> ReferencedEntities { get; }

        internal ITabularTableResolver TableResolver { get; }

        internal TabularRecordType(ConnectorType connectorType, DType recordType, ITabularTableResolver tableResolver, IList<ReferencedEntity> referencedEntities)
            : base(recordType)
        {
            ConnectorType = connectorType;
            TableResolver = tableResolver;
            ReferencedEntities = referencedEntities;
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            if (!base.TryGetBackingDType(name, out _))
            {
                type = null;
                return false;
            }

            ConnectorType cr = ConnectorType.Fields.First(ct => ct.Name == name);

            if (cr.ExternalTables?.Any() != true)
            {
                return base.TryGetFieldType(name, out type);
            }

            string tableName = cr.ExternalTables.First();

            try
            {
                TabularTableDescriptor ttd = TableResolver.ResolveTableAsync(tableName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

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
    }
}
