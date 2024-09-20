// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

#pragma warning disable SA1117

namespace Microsoft.PowerFx.Connectors
{
    internal class ExternalCdpDataSource : IExternalTabularDataSource
    {
        // recordType can be lazy, then fields has to be provided and will describe all fields non-lazily       
        public ExternalCdpDataSource(RecordType recordType, DName name, string datasetName, ServiceCapabilities serviceCapabilities, bool isReadOnly, IEnumerable<(string logicalName, string displayName, FormulaType type)> fields)
        {
            if (recordType._type.Kind == DKind.LazyRecord && fields == null)
            {
                throw new InvalidOperationException("When a LazyRecord is provided, fields cannot be null");
            }

            EntityName = name;
            ServiceCapabilities = serviceCapabilities;
            _delegationMetadata = serviceCapabilities?.ToDelegationMetadata(recordType._type);
            IsWritable = !isReadOnly;

            CdpEntityMetadataProvider metadataProvider = new CdpEntityMetadataProvider();
            CdpDataSourceMetadata tabularDataSourceMetadata = new CdpDataSourceMetadata(name.Value, datasetName);
            tabularDataSourceMetadata.LoadClientSemantics();
            metadataProvider.AddSource(name.Value, tabularDataSourceMetadata);

            DataEntityMetadataProvider = metadataProvider;
            IsConvertingDisplayNameMapping = false;
            PreviousDisplayNameMapping = null;

            List<ColumnMetadata> columns = null;

            if (fields != null)
            {
                DisplayNameMapping = new BidirectionalDictionary<string, string>(fields.Select(f => new KeyValuePair<string, string>(f.logicalName, f.displayName ?? f.logicalName)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                columns = fields.Select(f => new ColumnMetadata(f.logicalName, f.type._type, ToDataFormat(f.type), f.displayName, false /* is read-only */, false /* primary key */, false /* isRequired */,
                                                                ColumnCreationKind.UserProvided, ColumnVisibility.Default, f.logicalName, f.logicalName, f.logicalName, null, null)).ToList();
            }
            else
            {
                string GetDisplayName(string fieldName)
                {
                    DisplayNameProvider dnp = recordType._type.DisplayNameProvider;
                    return dnp == null || !dnp.TryGetDisplayName(new DName(fieldName), out DName displayName) ? fieldName : displayName.Value;
                }

                DisplayNameMapping = new BidirectionalDictionary<string, string>(recordType.FieldNames.Select(f => new KeyValuePair<string, string>(f, GetDisplayName(f))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                columns = recordType.FieldNames.Select(f => new ColumnMetadata(f, recordType.GetFieldType(f)._type, ToDataFormat(recordType.GetFieldType(f)), GetDisplayName(f), false /* is read-only */,
                                                                               false /* primary key */, false /* isRequired */, ColumnCreationKind.UserProvided, ColumnVisibility.Default, f, f, f, null, null)).ToList();
            }

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

        internal void SetType(DType type)
        {
            _type = type;
        }

        public ServiceCapabilities ServiceCapabilities { get; private set; }

        public IExternalTableMetadata TableMetadata => _tableMetadata;

        private readonly TableMetadata _tableMetadata;

        public IDelegationMetadata DelegationMetadata => _delegationMetadata;

        private readonly DelegationMetadata _delegationMetadata;

        private DType _type;

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

        public DName EntityName { get; }

        public DType Type => _type;

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
            return Enumerable.Empty<string>();
        }
    }
}
