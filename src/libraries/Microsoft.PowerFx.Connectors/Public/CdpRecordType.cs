// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpRecordType : RecordType
    {
        internal ConnectorType ConnectorType { get; }

        internal ICdpTableResolver TableResolver { get; }

        internal CdpRecordType(ConnectorType connectorType, ICdpTableResolver tableResolver, TableParameters tableParameters)
            : base(new CdpFieldAccessor(connectorType), connectorType.DisplayNameProvider, tableParameters)
        {
            ConnectorType = connectorType;
            TableResolver = tableResolver;
        }

        public bool TryGetFieldExternalTableName(string fieldName, out string tableName, out string foreignKey)
        {
            tableName = null;
            foreignKey = null;

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
            foreignKey = connectorType.ForeignKey;
            return true;
        }

        public override bool TryGetFieldType(string fieldName, out FormulaType type)
        {
            ConnectorType field = ConnectorType.Fields.FirstOrDefault(ct => ct.Name == fieldName);

            if (field == null)
            {
                type = null;
                return false;
            }

            if (field.ExternalTables?.Any() != true)
            {
                type = field.FormulaType;
                return true;
            }

            string tableName = field.ExternalTables.First();

            try
            {
                CdpTableDescriptor tableDescriptor = TableResolver.ResolveTableAsync(tableName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                type = tableDescriptor.RecordType;
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
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not RecordType otherRecordType || otherRecordType._type.Kind != _type.Kind)
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

        public override string TableSymbolName => ConnectorType.Name;

        public override IEnumerable<string> FieldNames => ConnectorType.Fields.Select(field => field.Name);
    }
}
