// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

#pragma warning disable SA1117

namespace Microsoft.PowerFx.Connectors
{
    internal class ExternalCdpDataSource : IExternalTabularDataSource
    {
        public ExternalCdpDataSource(ConnectorType connectorType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> displayNameMapping = null)
        {
            EntityName = name;
            ServiceCapabilities = serviceCapabilities;
            IsWritable = !isReadOnly;

            CdpEntityMetadataProvider metadataProvider = new CdpEntityMetadataProvider();
            CdpDataSourceMetadata tabularDataSourceMetadata = new CdpDataSourceMetadata(name.Value, datasetName);
            tabularDataSourceMetadata.LoadClientSemantics();
            metadataProvider.AddSource(name.Value, tabularDataSourceMetadata);

            DataEntityMetadataProvider = metadataProvider;
            IsConvertingDisplayNameMapping = false;
            DisplayNameMapping = displayNameMapping ?? new BidirectionalDictionary<string, string>();
            PreviousDisplayNameMapping = null;
            
            List<ColumnMetadata> columns = connectorType.Fields.Select((ConnectorType f) =>
                new ColumnMetadata(f.Name, f.FormulaType._type, ToDataFormat(f.FormulaType), f.DisplayName ?? f.Name, f.Permission == ConnectorPermission.PermissionReadOnly, 
                                   f.KeyType == ConnectorKeyType.Primary, f.IsRequired, ColumnCreationKind.UserProvided, ToColumnVisibility(f.Visibility), f.Name, f.Name, f.Name, null, null)).ToList();

            _tableMetadata = new TableMetadata(name, datasetName, isReadOnly, columns);
        }

        public ExternalCdpDataSource(RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> displayNameMapping = null)
        {
            EntityName = name;
            ServiceCapabilities = serviceCapabilities;
            IsWritable = !isReadOnly;

            CdpEntityMetadataProvider metadataProvider = new CdpEntityMetadataProvider();
            CdpDataSourceMetadata tabularDataSourceMetadata = new CdpDataSourceMetadata(name.Value, datasetName);
            tabularDataSourceMetadata.LoadClientSemantics();
            metadataProvider.AddSource(name.Value, tabularDataSourceMetadata);

            DataEntityMetadataProvider = metadataProvider;
            IsConvertingDisplayNameMapping = false;
            DisplayNameMapping = displayNameMapping ?? new BidirectionalDictionary<string, string>();
            PreviousDisplayNameMapping = null;

            List<ColumnMetadata> columns = recordType.FieldNames.Select((string fieldName) =>
            {
                FormulaType fieldType = recordType.GetFieldType(fieldName);
                return new ColumnMetadata(fieldName, fieldType._type, ToDataFormat(fieldType), fieldName /* display name */, false /* is read-only */, false /* primary key */, false /* isRequired */, 
                                          ColumnCreationKind.UserProvided, ColumnVisibility.Default, fieldName, fieldName, fieldName, null, null);
            }).ToList();

            _tableMetadata = new TableMetadata(name, datasetName, isReadOnly, columns);
        }

        private static ColumnVisibility ToColumnVisibility(Visibility v)
        {
            return v switch
            {
                Visibility.Internal => ColumnVisibility.Internal,
                Visibility.Important => ColumnVisibility.Important,
                Visibility.Advanced => ColumnVisibility.Advanced,
                Visibility.None => ColumnVisibility.Default,
                _ => throw new NotImplementedException($"Unknown visibility {v}")
            };
        }

        private static DataFormat? ToDataFormat(FormulaType ft)
        {
            return ft._type.Kind switch
            {
                DKind.Record or
                DKind.Table or
                DKind.OptionSetValue => DataFormat.Lookup,
                DKind.String or
                DKind.Decimal or
                DKind.Number or
                DKind.Currency => DataFormat.AllowedValues,
                _ => null

            };
        }

        internal ServiceCapabilities ServiceCapabilities;

        private readonly TableMetadata _tableMetadata;

        public TabularDataQueryOptions QueryOptions => new TabularDataQueryOptions(this);

        public string Name => EntityName.Value;

        public bool IsSelectable => ServiceCapabilities.IsSelectable;

        public bool IsDelegatable => ServiceCapabilities.IsDelegable;

        public bool IsPageable => ServiceCapabilities.IsPagable;

        public bool IsClearable => throw new NotImplementedException();

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public IExternalDataEntityMetadataProvider DataEntityMetadataProvider { get; }

        public DataSourceKind Kind => DataSourceKind.Connected;

        public IExternalTableMetadata TableMetadata => _tableMetadata;

        public IDelegationMetadata DelegationMetadata => throw new NotImplementedException();

        public DName EntityName { get; }

        public DType Type => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> DisplayNameMapping { get; protected set; }

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping { get; protected set; }

        public bool CanIncludeExpand(IExpandInfo expandToAdd) => true;

        public bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd) => true;

        public bool CanIncludeSelect(string selectColumnName) => TableMetadata != null && _tableMetadata.CanIncludeSelect(selectColumnName);

        public bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName) => true;

        public IReadOnlyList<string> GetKeyColumns()
        {            
            return _tableMetadata?.KeyColumns ?? new List<string>();
        }

        public IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }
    }    
}
