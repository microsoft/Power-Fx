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

        internal List<ReferencedEntity> ReferencedEntities { get; }

        internal ITabularTableResolver TableResolver { get; }

        internal TabularRecordType(ConnectorType connectorType, DType recordType, ITabularTableResolver tableResolver, List<ReferencedEntity> referencedEntities)
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

            if (!cr.ExternalTables.Any())
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

        public override bool Equals(object other)
        {
            if (other is not TabularRecordType otherRecordType)
            {
                return false;
            }

            if (_type.IsLazyType && otherRecordType._type.IsLazyType && _type.IsRecord == otherRecordType._type.IsRecord)
            {
                return _type.LazyTypeProvider.BackingFormulaType.Equals(otherRecordType._type.LazyTypeProvider.BackingFormulaType);
            }

            return _type.Equals(otherRecordType._type);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }
    }
}
