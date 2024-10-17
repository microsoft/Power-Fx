// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    // Implements a base data source, used in DType.AssociatedDataSources, itself in RecordType constructor to host a CDP record type
    internal class DataSourceInfo : IExternalTabularDataSource
    {
        // Key = field logical name, Value = foreign table logical name
        public readonly IReadOnlyDictionary<string, string> ColumnsWithRelationships;

        public DataSourceInfo(AggregateType recordType, DisplayNameProvider displayNameProvider, TableDelegationInfo delegationInfo)
        {
            EntityName = new DName(delegationInfo.TableName);
            IsWritable = !delegationInfo.IsReadOnly;
            _delegationInfo = delegationInfo;
            _type = recordType._type;
            RecordType = (RecordType)recordType;

            _displayNameMapping = new BidirectionalDictionary<string, string>(displayNameProvider.LogicalToDisplayPairs.ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value.Value ?? kvp.Key.Value));
            _externalDataEntityMetadataProvider = new InternalDataEntityMetadataProvider();
            _externalDataEntityMetadataProvider.AddSource(Name, new InternalDataEntityMetadata(delegationInfo.TableName, delegationInfo.DatasetName, _displayNameMapping));
            _externalTableMetadata = new InternalTableMetadata(RecordType, Name, Name, delegationInfo.IsReadOnly);
            _delegationMetadata = new DelegationMetadataBase(_type, new CompositeCapabilityMetadata(_type, GetCapabilityMetadata(recordType, delegationInfo)));
            _tabularDataQueryOptions = new TabularDataQueryOptions(this);
            _previousDisplayNameMapping = null;

            ColumnsWithRelationships = delegationInfo.ColumnsWithRelationships;
        }

        public TableDelegationInfo DelegationInfo => _delegationInfo;

        private readonly TableDelegationInfo _delegationInfo;

        private readonly DType _type;

        private readonly InternalDataEntityMetadataProvider _externalDataEntityMetadataProvider;

        private readonly InternalTableMetadata _externalTableMetadata;

        private readonly DelegationMetadataBase _delegationMetadata;

        private readonly TabularDataQueryOptions _tabularDataQueryOptions;

        private readonly BidirectionalDictionary<string, string> _displayNameMapping;

        private readonly BidirectionalDictionary<string, string> _previousDisplayNameMapping;

        TabularDataQueryOptions IExternalTabularDataSource.QueryOptions => _tabularDataQueryOptions;

        public string Name => EntityName.Value;

        public DName EntityName { get; }

        public bool IsSelectable => _delegationInfo.IsSelectable;

        public bool IsDelegatable => _delegationInfo.IsDelegable;

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public bool IsClearable => throw new System.NotImplementedException();

        IExternalDataEntityMetadataProvider IExternalDataSource.DataEntityMetadataProvider => _externalDataEntityMetadataProvider;

        DataSourceKind IExternalDataSource.Kind => DataSourceKind.Connected;

        IExternalTableMetadata IExternalDataSource.TableMetadata => _externalTableMetadata;

        IDelegationMetadata IExternalDataSource.DelegationMetadata => _delegationMetadata;

        DType IExternalEntity.Type => _type;

        public RecordType RecordType { get; }

        public bool IsPageable => _delegationInfo.PagingCapabilities.IsOnlyServerPagable || IsDelegatable;

        public bool IsConvertingDisplayNameMapping => false;

        BidirectionalDictionary<string, string> IDisplayMapped<string>.DisplayNameMapping => _displayNameMapping;

        BidirectionalDictionary<string, string> IDisplayMapped<string>.PreviousDisplayNameMapping => _previousDisplayNameMapping;

        public bool HasCachedCountRows => false;

        public IReadOnlyList<string> GetKeyColumns() => _externalTableMetadata?.KeyColumns ?? new List<string>();

        IEnumerable<string> IExternalTabularDataSource.GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(string selectColumnName) => _externalTableMetadata != null && _externalTableMetadata.CanIncludeSelect(selectColumnName);

        bool IExternalTabularDataSource.CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName)
        {
            throw new NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        private static List<OperationCapabilityMetadata> GetCapabilityMetadata(AggregateType recordType, TableDelegationInfo delegationInfo)
        {
            DType type = recordType._type;

            DPath GetDPath(string prop) => DPath.Root.Append(new DName(prop));

            void AddOrUpdate(Dictionary<DPath, DelegationCapability> dic, string prop, DelegationCapability capability)
            {
                DPath dPath = GetDPath(prop);

                if (!dic.TryGetValue(dPath, out DelegationCapability existingCapability))
                {
                    dic.Add(dPath, capability);
                }
                else
                {
                    dic[dPath] = new DelegationCapability(existingCapability.Capabilities | capability.Capabilities);
                }
            }

            Dictionary<DPath, DelegationCapability> groupByRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (delegationInfo?.GroupRestriction?.UngroupableProperties != null)
            {
                foreach (string ungroupableProperty in delegationInfo.GroupRestriction.UngroupableProperties)
                {
                    AddOrUpdate(groupByRestrictions, ungroupableProperty, DelegationCapability.Group);
                }
            }

            Dictionary<DPath, DPath> oDataReplacements = new Dictionary<DPath, DPath>();

            FilterOpMetadata filterOpMetadata = new CdpFilterOpMetadata(recordType, delegationInfo);
            GroupOpMetadata groupOpMetadata = new GroupOpMetadata(type, groupByRestrictions);
            ODataOpMetadata oDataOpMetadata = new ODataOpMetadata(type, oDataReplacements);

            List<OperationCapabilityMetadata> metadataList = new List<OperationCapabilityMetadata>()
            {
                filterOpMetadata,
                groupOpMetadata,
                oDataOpMetadata
            };

            if (delegationInfo?.SortRestriction != null)
            {
                Dictionary<DPath, DelegationCapability> sortRestrictions = new Dictionary<DPath, DelegationCapability>();

                if (delegationInfo?.SortRestriction?.UnsortableProperties != null)
                {
                    foreach (string unsortableProperty in delegationInfo.SortRestriction.UnsortableProperties)
                    {
                        AddOrUpdate(sortRestrictions, unsortableProperty, DelegationCapability.Sort);
                    }
                }

                if (delegationInfo?.SortRestriction?.AscendingOnlyProperties != null)
                {
                    foreach (string ascendingOnlyProperty in delegationInfo.SortRestriction.AscendingOnlyProperties)
                    {
                        AddOrUpdate(sortRestrictions, ascendingOnlyProperty, DelegationCapability.SortAscendingOnly);
                    }
                }

                SortOpMetadata sortOpMetadata = new SortOpMetadata(type, sortRestrictions);

                metadataList.Add(sortOpMetadata);
            }

            return metadataList;
        }

        internal static DPath GetReplacementPath(string alias, DPath currentColumnPath)
        {
            if (alias.Contains("/"))
            {
                var fullPath = DPath.Root;

                foreach (var name in alias.Split('/'))
                {
                    fullPath = fullPath.Append(new DName(name));
                }

                return fullPath;
            }
            else
            {
                // Task 5593666: This is temporary to not cause regressions while sharepoint switches to using full query param
                return currentColumnPath.Append(new DName(alias));
            }
        }
    }
}
