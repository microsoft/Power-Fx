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
    internal class InternalTableParameters : IExternalTabularDataSource
    {
        public const string IsChoiceValue = "Value";

        public readonly Dictionary<string, string> ColumnsWithRelationships;

        public InternalTableParameters(AggregateType recordType, DisplayNameProvider displayNameProvider, TableDelegationInfo tableParameters)
        {
            string GetDisplayName(string fieldName) => displayNameProvider == null || !displayNameProvider.TryGetDisplayName(new DName(fieldName), out DName displayName) ? fieldName : displayName.Value;

            EntityName = new DName(tableParameters.TableName);
            IsWritable = !tableParameters.IsReadOnly;
            _tableParameters = tableParameters;
            _type = recordType._type;
            RecordType = (RecordType)recordType;

            IEnumerable<string> fieldNames = displayNameProvider.LogicalToDisplayPairs.Select(pair => pair.Key.Value);

            _displayNameMapping = new BidirectionalDictionary<string, string>(fieldNames.Select(f => new KeyValuePair<string, string>(f, GetDisplayName(f))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            _externalDataEntityMetadataProvider = new InternalDataEntityMetadataProvider();
            _externalDataEntityMetadataProvider.AddSource(Name, new InternalDataEntityMetadata(tableParameters.TableName, tableParameters.DatasetName, _displayNameMapping));
            _externalTableMetadata = new InternalTableMetadata(RecordType, Name, Name, tableParameters.IsReadOnly);
            _delegationMetadata = new DelegationMetadataBase(_type, new CompositeCapabilityMetadata(_type, GetCapabilityMetadata(recordType, tableParameters)));
            _tabularDataQueryOptions = new TabularDataQueryOptions(this);
            _previousDisplayNameMapping = null;

            ColumnsWithRelationships = tableParameters.ColumnsWithRelationships;
        }

        public TableDelegationInfo TableParameters => _tableParameters;

        private readonly TableDelegationInfo _tableParameters;

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

        public bool IsSelectable => _tableParameters.SelectionRestriction == null ? false : _tableParameters.SelectionRestriction.IsSelectable;

        public bool IsDelegatable => _tableParameters.IsDelegable;

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

        public bool IsPageable => _tableParameters.PagingCapabilities.IsOnlyServerPagable || IsDelegatable;

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
            throw new System.NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new System.NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new System.NotImplementedException();
        }

        private static List<OperationCapabilityMetadata> GetCapabilityMetadata(AggregateType recordType, TableDelegationInfo tableParameters)
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

            if (tableParameters?.GroupRestriction?.UngroupableProperties != null)
            {
                foreach (string ungroupableProperty in tableParameters.GroupRestriction.UngroupableProperties)
                {
                    AddOrUpdate(groupByRestrictions, ungroupableProperty, DelegationCapability.Group);
                }
            }

            Dictionary<DPath, DelegationCapability> sortRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (tableParameters?.SortRestriction?.UnsortableProperties != null)
            {
                foreach (string unsortableProperty in tableParameters.SortRestriction.UnsortableProperties)
                {
                    AddOrUpdate(sortRestrictions, unsortableProperty, DelegationCapability.Sort);
                }
            }

            if (tableParameters?.SortRestriction?.AscendingOnlyProperties != null)
            {
                foreach (string ascendingOnlyProperty in tableParameters.SortRestriction.AscendingOnlyProperties)
                {
                    AddOrUpdate(sortRestrictions, ascendingOnlyProperty, DelegationCapability.SortAscendingOnly);
                }
            }

            Dictionary<DPath, DPath> oDataReplacements = new Dictionary<DPath, DPath>();

            FilterOpMetadata filterOpMetadata = new FilterOpMetadata(recordType, tableParameters);
            GroupOpMetadata groupOpMetadata = new GroupOpMetadata(type, groupByRestrictions);
            ODataOpMetadata oDataOpMetadata = new ODataOpMetadata(type, oDataReplacements);
            SortOpMetadata sortOpMetadata = new SortOpMetadata(type, sortRestrictions);

            return new List<OperationCapabilityMetadata>()
            {
                filterOpMetadata,
                groupOpMetadata,
                oDataOpMetadata,
                sortOpMetadata
            };
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
