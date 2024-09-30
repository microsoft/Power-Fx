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

        private List<string> _fieldNames;

        internal ICdpTableResolver TableResolver { get; }

        internal CdpRecordType(ConnectorType connectorType, ICdpTableResolver tableResolver, ServiceCapabilities2 tableCapabilities)
            : base(connectorType.DisplayNameProvider, tableCapabilities)
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
            if (!base.TryGetBackingDType(fieldName, out _))
            {
                type = null;
                return false;
            }

            ConnectorType field = ConnectorType.Fields.FirstOrDefault(ct => ct.Name == fieldName);   

            if (field.ExternalTables?.Any() != true)
            {
                type = field.FormulaType;
                return true;
            }

            string tableName = field.ExternalTables.First();

            try
            {
                CdpTableDescriptor ttd = TableResolver.ResolveTableAsync(tableName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                type = ttd.FormulaType;
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
            if (other is not RecordType recordType || recordType._type.Kind != Core.Types.DKind.LazyRecord)
            {
                return false;
            }

            // $$$ TO BE TESTED
            throw new Exception();
            return true;
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        public override string TableSymbolName => ConnectorType.Name;

        public override IEnumerable<string> FieldNames
        {
            get
            {
                _fieldNames ??= ConnectorType.Fields.Select(field => field.Name).ToList();
                return _fieldNames;
            }
        }
    }
}
