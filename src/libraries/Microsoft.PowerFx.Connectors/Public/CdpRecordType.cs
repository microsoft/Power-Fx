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

        internal CdpRecordType(ConnectorType connectorType, ICdpTableResolver tableResolver, TableDelegationInfo delegationInfo)
            : base(connectorType.DisplayNameProvider, delegationInfo)
        {
            ConnectorType = connectorType;
            TableResolver = tableResolver;
        }

        public bool TryGetFieldExternalTableName(string fieldName, out string tableName, out string foreignKey)
        {
            tableName = null;
            foreignKey = null;

            ConnectorType connectorType = ConnectorType.Fields.First(ct => ct.Name == fieldName);

            if (connectorType == null || connectorType.ExternalTables?.Any() != true)
            {
                return false;
            }

            tableName = connectorType.ExternalTables.First().Key;
            foreignKey = connectorType.ForeignKey;
            return true;
        }

        public override bool TryGetUnderlyingFieldType(string name, out FormulaType type) => TryGetFieldType(name, true, out type);

        public override bool TryGetFieldType(string name, out FormulaType type) => TryGetFieldType(name, false, out type);

        private bool TryGetFieldType(string fieldName, bool ignorelationship, out FormulaType type)
        {
            ConnectorType field = ConnectorType.Fields.FirstOrDefault(ct => ct.Name == fieldName);

            if (field == null)
            {
                type = null;
                return false;
            }

            if (field.ExternalTables?.Any() != true || ignorelationship)
            {
                type = field.FormulaType;
                return true;
            }

            KeyValuePair<string, ConnectorType> foreignTable = field.ExternalTables.First();

            if (foreignTable.Value != null)
            {
                type = foreignTable.Value.FormulaType;
                return true;
            }

            try
            {
                ConnectorType connectorType = TableResolver.ResolveTableAsync(foreignTable.Key, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                field.ExternalTables[foreignTable.Key] = connectorType;
                type = connectorType.FormulaType;
                return true;
            }
            catch (Exception ex)
            {
                TableResolver?.Logger.LogException(ex, $"Cannot resolve external table {foreignTable.Key}");
                throw;
            }
        }

        public override bool Equals(object other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not CdpRecordType otherRecordType)
            {
                return false;
            }

            return ConnectorType.Equals(otherRecordType.ConnectorType);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        public override string TableSymbolName => ConnectorType.Name;

        public override IEnumerable<string> FieldNames => ConnectorType.Fields.Select(field => field.Name);
    }
}
