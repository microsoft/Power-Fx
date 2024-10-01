// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal class InternalTableParameters : IExternalTabularDataSource
    {
        public const string IsChoiceValue = "Value";

        public readonly Dictionary<string, string> ColumnsWithRelationships;

        public InternalTableParameters(TableParameters tableParameters)
        {
            DType type = tableParameters.RecordType._type;

            string GetDisplayName(string fieldName)
            {
                DisplayNameProvider dnp = type.DisplayNameProvider;
                return dnp == null || !dnp.TryGetDisplayName(new DName(fieldName), out DName displayName) ? fieldName : displayName.Value;
            }

            DType GetFieldType(string fieldName) => type.TryGetType(new DName(fieldName), out var dType) ? dType : DType.ObjNull /* Blank */;

            DataFormat? ToDataFormat(DType dType)
            {
                return dType.Kind switch
                {
                    DKind.Record or DKind.Table or DKind.OptionSetValue => DataFormat.Lookup,
                    DKind.String or DKind.Decimal or DKind.Number or DKind.Currency => DataFormat.AllowedValues,
                    _ => null
                };
            }

            EntityName = new DName(tableParameters.TableName);
            IsWritable = !tableParameters.IsReadOnly;
            _tableParameters = tableParameters;
            _type = type;

            IEnumerable<string> fieldNames = type.GetRootFieldNames().Select(name => name.Value);

            _displayNameMapping = new BidirectionalDictionary<string, string>(fieldNames.Select(f => new KeyValuePair<string, string>(f, GetDisplayName(f))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            List<ColumnMetadata> columns = fieldNames.Select(f => new ColumnMetadata(f, GetFieldType(f), ToDataFormat(GetFieldType(f)), GetDisplayName(f), false /* is read-only */, false /* primary key */, false /* isRequired */, ColumnCreationKind.UserProvided, ColumnVisibility.Default, f, f, f, null, null)).ToList();

            _externalTableMetadata = new InternalTableMetadata(Name, Name, tableParameters.IsReadOnly, columns);
            _externalDataEntityMetadataProvider = new InternalDataEntityMetadataProvider();
            _externalDataEntityMetadataProvider.AddSource(Name, new InternalDataEntityMetadata(tableParameters.TableName, tableParameters.DatasetName, _displayNameMapping));
            _delegationMetadata = new DelegationMetadataBase(type, new CompositeCapabilityMetadata(type, GetCapabilityMetadata(type, tableParameters)));
            _tabularDataQueryOptions = new TabularDataQueryOptions(this);
            _previousDisplayNameMapping = null;

            ColumnsWithRelationships = tableParameters.ColumnsWithRelationships;
        }

        private readonly TableParameters _tableParameters;

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

        public bool IsDelegatable => (_tableParameters.SortRestriction != null) ||
                                     (_tableParameters.FilterRestriction != null) ||
                                     (_tableParameters.FilterFunctions != null);

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public bool IsClearable => throw new System.NotImplementedException();

        IExternalDataEntityMetadataProvider IExternalDataSource.DataEntityMetadataProvider => _externalDataEntityMetadataProvider;

        DataSourceKind IExternalDataSource.Kind => DataSourceKind.Connected;

        IExternalTableMetadata IExternalDataSource.TableMetadata => _externalTableMetadata;

        IDelegationMetadata IExternalDataSource.DelegationMetadata => _delegationMetadata;

        DType IExternalEntity.Type => _type;

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

        private static List<OperationCapabilityMetadata> GetCapabilityMetadata(DType type, TableParameters tableParameters)
        {
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

            List<OperationCapabilityMetadata> capabilities = new List<OperationCapabilityMetadata>();

            Dictionary<DPath, DelegationCapability> columnRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (tableParameters?.FilterRestriction?.NonFilterableProperties != null)
            {
                foreach (string nonFilterableProperties in tableParameters.FilterRestriction.NonFilterableProperties)
                {
                    AddOrUpdate(columnRestrictions, nonFilterableProperties, DelegationCapability.Filter);
                }
            }

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>();

            if (tableParameters?.ColumnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase> kvp in tableParameters.ColumnsCapabilities)
                {
                    if (kvp.Value is ColumnCapabilities cc)
                    {
                        DelegationCapability columnDelegationCapability = DelegationCapability.None;

                        if (cc.Capabilities?.FilterFunctions != null)
                        {
                            foreach (string columnFilterFunction in cc.Capabilities.FilterFunctions)
                            {
                                if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(columnFilterFunction, out DelegationCapability filterFunctionCapability))
                                {
                                    columnDelegationCapability |= filterFunctionCapability;
                                }
                            }
                        }

                        if (columnDelegationCapability.Capabilities != DelegationCapability.None && !columnRestrictions.ContainsKey(GetDPath(kvp.Key)))
                        {
                            AddOrUpdate(columnCapabilities, kvp.Key, columnDelegationCapability | DelegationCapability.Filter);
                        }

                        if (cc.Capabilities.IsChoice == true && !columnRestrictions.ContainsKey(GetDPath(IsChoiceValue)))
                        {
                            AddOrUpdate(columnCapabilities, IsChoiceValue, columnDelegationCapability | DelegationCapability.Filter);
                        }
                    }
                    else if (kvp.Value is ComplexColumnCapabilities)
                    {
                        throw new NotImplementedException($"ComplexColumnCapabilities not supported yet");
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown ColumnCapabilitiesBase, type {kvp.Value.GetType().Name}");
                    }
                }
            }

            DelegationCapability filterFunctionSupportedByAllColumns = DelegationCapability.None;

            if (tableParameters?.FilterFunctions != null)
            {
                foreach (string globalFilterFunction in tableParameters.FilterFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalFilterFunction, out DelegationCapability globalFilterFunctionCapability))
                    {
                        filterFunctionSupportedByAllColumns |= globalFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            DelegationCapability? filterFunctionsSupportedByTable = null;

            if (tableParameters?.FilterSupportedFunctions != null)
            {
                filterFunctionsSupportedByTable = DelegationCapability.None;

                foreach (string globalSupportedFilterFunction in tableParameters.FilterSupportedFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalSupportedFilterFunction, out DelegationCapability globalSupportedFilterFunctionCapability))
                    {
                        filterFunctionsSupportedByTable |= globalSupportedFilterFunctionCapability | DelegationCapability.Filter;
                    }
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

            Dictionary<DPath, DPath> oDataReplacements = new Dictionary<DPath, DPath>();

            if (tableParameters?.ColumnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase> kvp in tableParameters.ColumnsCapabilities)
                {
                    if (kvp.Value is ColumnCapabilities cc)
                    {
                        DPath columnPath = GetDPath(kvp.Key);
                        DelegationCapability columnDelegationCapability = DelegationCapability.None;

                        if (cc.Capabilities.IsChoice == true)
                        {
                            oDataReplacements.Add(columnPath.Append(GetDPath(IsChoiceValue)), columnPath);
                        }

                        if (!string.IsNullOrEmpty(cc.Capabilities.QueryAlias))
                        {
                            oDataReplacements.Add(columnPath, GetReplacementPath(cc.Capabilities.QueryAlias, columnPath));
                        }
                    }
                    else if (kvp.Value is ComplexColumnCapabilities)
                    {
                        throw new NotImplementedException($"ComplexColumnCapabilities not supported yet");
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown ColumnCapabilitiesBase, type {kvp.Value.GetType().Name}");
                    }
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

            FilterOpMetadata filterOpMetadata = new FilterOpMetadata(type, columnRestrictions, columnCapabilities, filterFunctionSupportedByAllColumns, filterFunctionsSupportedByTable);
            GroupOpMetadata groupOpMetadata = new GroupOpMetadata(type, groupByRestrictions);
            ODataOpMetadata oDataOpMetadata = new ODataOpMetadata(type, oDataReplacements);
            SortOpMetadata sortOpMetadata = new SortOpMetadata(type, sortRestrictions);

            capabilities.Add(filterOpMetadata);
            capabilities.Add(groupOpMetadata);
            capabilities.Add(oDataOpMetadata);
            capabilities.Add(sortOpMetadata);

            return capabilities;
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
