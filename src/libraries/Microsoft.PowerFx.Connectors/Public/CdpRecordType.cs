// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpRecordType : RecordType, ICDPAggregateMetadata
    {
        internal ConnectorType ConnectorType { get; }

        internal ICdpTableResolver TableResolver { get; }

        private readonly IEnumerable<string> _primaryKeyNames;

        private readonly IEnumerable<CDPMetadataItem> _metadataItems;
        private readonly IEnumerable<CDPSensitivityLabelInfo> _sensitivityLabels;

        internal CdpRecordType(ConnectorType connectorType, ICdpTableResolver tableResolver, TableDelegationInfo delegationInfo)
            : base(connectorType.DisplayNameProvider, delegationInfo)
        {
            ConnectorType = connectorType;
            TableResolver = tableResolver;

            _primaryKeyNames = delegationInfo.PrimaryKeyNames;

            // build metadata items first.
            _metadataItems = BuildMetadataItems();

            // build sensitivity labels by flattening the already‐cached metadata items
            _sensitivityLabels = BuildSensitivityLabel();
        }

        public bool TryGetSensitivityLabelInfo(out IEnumerable<CDPSensitivityLabelInfo> sensitivityLabelInfo)
        {
            if (_sensitivityLabels.Any())
            {
                sensitivityLabelInfo = _sensitivityLabels;
                return true;
            }

            sensitivityLabelInfo = null;
            return false;
        }

        public bool TryGetMetadataItems(out IEnumerable<CDPMetadataItem> cdpMetadataItems)
        {
            if (_metadataItems.Any())
            {
                cdpMetadataItems = _metadataItems;
                return true;
            }

            cdpMetadataItems = null;
            return false;
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

            tableName = connectorType.ExternalTables.First();
            foreignKey = connectorType.ForeignKey;
            return true;
        }

        public override bool TryGetUnderlyingFieldType(string name, out FormulaType type) => TryGetFieldType(name, true, out type);

        public override bool TryGetFieldType(string name, out FormulaType type) => TryGetFieldType(name, true, out type);

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

            string tableName = field.ExternalTables.First();

            try
            {
                if (TableResolver == null)
                {
                    throw new InvalidOperationException("TableResolver is not set.");
                }

                ConnectorType connectorType = TableResolver.ResolveTableAsync(tableName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                type = connectorType.FormulaType;
                return true;
            }
            catch (Exception ex)
            {
                TableResolver?.Logger?.LogException(ex, $"Cannot resolve external table {tableName}");
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

        [Obsolete]
        public override bool TryGetPrimaryKeyFieldName(out IEnumerable<string> primaryKeyNames)
        {
            primaryKeyNames = _primaryKeyNames;
            return primaryKeyNames != null && primaryKeyNames.Any();
        }

        public override IEnumerable<string> FieldNames => ConnectorType.Fields.Select(field => field.Name);

        private IEnumerable<CDPMetadataItem> BuildMetadataItems()
        {
            if (ConnectorType?.Fields == null)
            {
                return Array.Empty<CDPMetadataItem>();
            }

            return ConnectorType.Fields
                .Where(f => f.FieldMetadata?.Any() == true)
                .Select(f => new CDPMetadataItem
                {
                    Name = f.Name,
                    SensitivityLabels = f.FieldMetadata
                })
                .ToList();
        }

        /// <summary>
        /// build sensitivity labels by flattening the already‐cached metadata items.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<CDPSensitivityLabelInfo> BuildSensitivityLabel()
        {
            return _metadataItems
                .SelectMany(mi => mi.SensitivityLabels)
                .ToList();
        }
    }
}
