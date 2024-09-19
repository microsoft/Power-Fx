// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

#pragma warning disable SA1117 // Parameters should be on separate lines

// <summary>
// Source code copied from PowerApps-Client repo
// src/Language/PowerFx.Dataverse.Parser/Importers/DataDescription/ServiceCapabilities.cs
// </summary>

namespace Microsoft.PowerFx.Connectors
{
    public sealed class ServiceCapabilities : IColumnsCapabilities
    {
#pragma warning disable SA1300 // Element should begin with upper-case letter

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ColumnsCapabilities)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ColumnCapabilitiesBase> _columnsCapabilities { get; private set; }

#pragma warning restore SA1300 // Element should begin with upper-case letter

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SortRestrictions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly SortRestriction SortRestriction;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterRestrictions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly FilterRestriction FilterRestriction;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SelectRestriction)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly SelectionRestriction SelectionRestriction;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.GroupRestriction)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly GroupRestriction GroupRestriction;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterFunctions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly IEnumerable<string> FilterFunctions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterFunctionSupport)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly IEnumerable<string> FilterSupportedFunctions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ServerPagingOptions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string> ServerPagingOptions => PagingCapabilities.ServerPagingOptions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.IsOnlyServerPagable)]
        public bool IsOnlyServerPageable => PagingCapabilities.IsOnlyServerPagable;

        [JsonIgnore]
        public readonly PagingCapabilities PagingCapabilities;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.IsPageable)]
        public readonly bool IsPagable;

        [JsonIgnore]
        public readonly bool SupportsDataverseOffline;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.IsDelegable)]
        public readonly bool IsDelegable;

        [JsonIgnore]
        public readonly bool IsSelectable;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ODataVersion)]
        public readonly int ODataVersion;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.SupportsRecordPermission)]
        public readonly bool SupportsRecordPermission;

        public const int CurrentODataVersion = 4;

        public ServiceCapabilities(SortRestriction sortRestriction, FilterRestriction filterRestriction, SelectionRestriction selectionRestriction, GroupRestriction groupRestriction, IEnumerable<string> filterFunctions, 
                                   IEnumerable<string> filterSupportedFunctions, PagingCapabilities pagingCapabilities, bool recordPermissionCapabilities, int oDataVersion = CurrentODataVersion, bool supportsDataverseOffline = false)
        {
            Contracts.AssertValueOrNull(sortRestriction);
            Contracts.AssertValueOrNull(filterRestriction);
            Contracts.AssertValueOrNull(selectionRestriction);
            Contracts.AssertValueOrNull(groupRestriction);
            Contracts.AssertValueOrNull(filterFunctions);
            Contracts.AssertValueOrNull(filterSupportedFunctions);
            Contracts.AssertValue(pagingCapabilities);

            SortRestriction = sortRestriction;
            FilterRestriction = filterRestriction;
            FilterFunctions = filterFunctions;
            PagingCapabilities = pagingCapabilities;
            SelectionRestriction = selectionRestriction;
            GroupRestriction = groupRestriction;
            IsDelegable = (SortRestriction != null) || (FilterRestriction != null) || (FilterFunctions != null);
            IsPagable = PagingCapabilities.IsOnlyServerPagable || IsDelegable;
            SupportsDataverseOffline = supportsDataverseOffline;
            FilterSupportedFunctions = filterSupportedFunctions;
            IsSelectable = SelectionRestriction == null ? false : selectionRestriction.IsSelectable;
            _columnsCapabilities = null;
            ODataVersion = oDataVersion;
            SupportsRecordPermission = recordPermissionCapabilities;
        }

        public static ServiceCapabilities Default(RecordType recordType)
        {
            ServiceCapabilities serviceCapabilities = new ServiceCapabilities(
                new SortRestriction(new List<string>() /* unsortableProperties */, new List<string>() /* ascendingOnlyProperties */),
                new FilterRestriction(new List<string>() /* requiredProperties */, new List<string>() /* nonFilterableProperties */),
                new SelectionRestriction(true /* isSelectable */),
                new GroupRestriction(new List<string>() /* ungroupableProperties */),
                ColumnCapabilities.DefaultFilterFunctionSupport, // filterFunctions
                ColumnCapabilities.DefaultFilterFunctionSupport, // filterSupportedFunctions
                new PagingCapabilities(false /* isOnlyServerPagable */, new string[0] /* serverPagingOptions */),
                true); // recordPermissionCapabilities                                

            serviceCapabilities.AddColumnCapabilities(recordType);

            return serviceCapabilities;
        }

        internal DelegationMetadata ToDelegationMetadata(DType type)
        {
            List<OperationCapabilityMetadata> capabilities = new List<OperationCapabilityMetadata>();

            Dictionary<DPath, DelegationCapability> columnRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (FilterRestriction?.NonFilterableProperties != null)
            {
                foreach (string nonFilterableProperties in FilterRestriction.NonFilterableProperties)
                {
                    AddOrUpdate(columnRestrictions, nonFilterableProperties, DelegationCapability.Filter);
                }
            }

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>();

            if (_columnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase> kvp in _columnsCapabilities)
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

                        if (cc.Capabilities.IsChoice == true && !columnRestrictions.ContainsKey(GetDPath(CapabilityConstants.IsChoiceValue)))
                        {
                            AddOrUpdate(columnCapabilities, CapabilityConstants.IsChoiceValue, columnDelegationCapability | DelegationCapability.Filter);
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

            if (FilterFunctions != null)
            {
                foreach (string globalFilterFunction in FilterFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalFilterFunction, out DelegationCapability globalFilterFunctionCapability))
                    {
                        filterFunctionSupportedByAllColumns |= globalFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            DelegationCapability? filterFunctionsSupportedByTable = null;

            if (FilterSupportedFunctions != null)
            {
                filterFunctionsSupportedByTable = DelegationCapability.None;

                foreach (string globalSupportedFilterFunction in FilterSupportedFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalSupportedFilterFunction, out DelegationCapability globalSupportedFilterFunctionCapability))
                    {
                        filterFunctionsSupportedByTable |= globalSupportedFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            Dictionary<DPath, DelegationCapability> groupByRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (GroupRestriction?.UngroupableProperties != null)
            {
                foreach (string ungroupableProperty in GroupRestriction.UngroupableProperties)
                {
                    AddOrUpdate(groupByRestrictions, ungroupableProperty, DelegationCapability.Group);
                }
            }
            
            Dictionary<DPath, DPath> oDataReplacements = new Dictionary<DPath, DPath>();

            if (_columnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase> kvp in _columnsCapabilities)
                {
                    if (kvp.Value is ColumnCapabilities cc)
                    {
                        DPath columnPath = GetDPath(kvp.Key);
                        DelegationCapability columnDelegationCapability = DelegationCapability.None;

                        if (cc.Capabilities.IsChoice == true)
                        {
                            oDataReplacements.Add(columnPath.Append(GetDPath(CapabilityConstants.IsChoiceValue)), columnPath);
                        }

                        if (!string.IsNullOrEmpty(cc.Capabilities.QueryAlias))
                        {
                            oDataReplacements.Add(columnPath, DelegationMetadata.GetReplacementPath(cc.Capabilities.QueryAlias, columnPath));
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

            if (SortRestriction?.UnsortableProperties != null)
            {
                foreach (string unsortableProperty in SortRestriction.UnsortableProperties)
                {
                    AddOrUpdate(sortRestrictions, unsortableProperty, DelegationCapability.Sort);
                }
            }

            if (SortRestriction?.AscendingOnlyProperties != null)
            {
                foreach (string ascendingOnlyProperty in SortRestriction.AscendingOnlyProperties)
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

            return new DelegationMetadata(type, capabilities);
        }

        private DPath GetDPath(string prop) => DPath.Root.Append(new DName(prop));

        private void AddOrUpdate(Dictionary<DPath, DelegationCapability> dic, string prop, DelegationCapability capability)
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

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _columnsCapabilities ??= new Dictionary<string, ColumnCapabilitiesBase>();            
            _columnsCapabilities.Add(name, capability);
        }

        public void AddColumnCapabilities(RecordType recordType)
        {
            foreach (string fieldName in recordType.FieldNames)
            {
                AddColumnCapability(fieldName, ColumnCapabilities.DefaultCdsColumnCapabilities);
            }
        }

        // From PowerApps-Client repo, src\AppMagic\dll\AuthoringCore\Importers\DataDescription\ServiceCapabilitiesParser.cs
        public static ServiceCapabilities ParseTableCapabilities(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            FilterRestriction filterRestriction = ParseFilterRestriction(capabilitiesMetaData);
            SortRestriction sortRestriction = ParseSortRestriction(capabilitiesMetaData);
            SelectionRestriction selectionRestriction = ParseSelectionRestriction(capabilitiesMetaData);
            GroupRestriction groupRestriction = ParseGroupRestriction(capabilitiesMetaData);
            string[] filterFunctions = ParseFilterFunctions(capabilitiesMetaData);
            string[] filterSupportedFunctions = ParseFilterSupportedFunctions(capabilitiesMetaData);
            PagingCapabilities pagingCapabilities = ParsePagingCapabilities(capabilitiesMetaData);
            bool recordPermissionCapabilities = ParseRecordPermissionCapabilities(capabilitiesMetaData);
            int oDataVersion = capabilitiesMetaData.GetInt(CapabilityConstants.ODataversionOption, defaultValue: CurrentODataVersion);

            if (oDataVersion > CurrentODataVersion || oDataVersion < 3)
            {
                throw new PowerFxConnectorException("Table capabilities specifies an unsupported oDataVersion");
            }

            return new ServiceCapabilities(sortRestriction, filterRestriction, selectionRestriction, groupRestriction, filterFunctions, filterSupportedFunctions, pagingCapabilities, recordPermissionCapabilities, oDataVersion);
        }

        private static FilterRestriction ParseFilterRestriction(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            IDictionary<string, IOpenApiAny> filterRestritionMetaData = capabilitiesMetaData.GetObject(CapabilityConstants.FilterRestrictions);
            return filterRestritionMetaData?.GetBool(CapabilityConstants.Filterable) == true
                    ? new FilterRestriction(filterRestritionMetaData.GetList(CapabilityConstants.FilterRequiredProperties), filterRestritionMetaData.GetList(CapabilityConstants.NonFilterableProperties))
                    : null;
        }

        private static SortRestriction ParseSortRestriction(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            IDictionary<string, IOpenApiAny> sortRestrictionMetaData = capabilitiesMetaData.GetObject(CapabilityConstants.SortRestrictions);
            return sortRestrictionMetaData?.GetBool(CapabilityConstants.Sortable) == true
                    ? new SortRestriction(sortRestrictionMetaData.GetList(CapabilityConstants.UnsortableProperties), sortRestrictionMetaData.GetList(CapabilityConstants.AscendingOnlyProperties))
                    : null;
        }

        private static SelectionRestriction ParseSelectionRestriction(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            IDictionary<string, IOpenApiAny> selectRestrictionsMetadata = capabilitiesMetaData.GetObject(CapabilityConstants.SelectionRestriction);
            return selectRestrictionsMetadata == null
                    ? null
                    : new SelectionRestriction(selectRestrictionsMetadata.GetBool(CapabilityConstants.Selectable, "selectable property is mandatory and not found."));
        }

        private static GroupRestriction ParseGroupRestriction(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            IDictionary<string, IOpenApiAny> groupRestrictionMetaData = capabilitiesMetaData.GetObject(CapabilityConstants.GroupRestriction);
            return groupRestrictionMetaData == null
                    ? null
                    : new GroupRestriction(groupRestrictionMetaData.GetList(CapabilityConstants.UngroupableProperties));
        }

        internal static string[] ParseFilterFunctions(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            return capabilitiesMetaData.GetArray(CapabilityConstants.FilterFunctions);
        }

        private static string[] ParseFilterSupportedFunctions(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            return capabilitiesMetaData.GetArray(CapabilityConstants.FilterFunctionSupport);
        }

        private static PagingCapabilities ParsePagingCapabilities(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            return new PagingCapabilities(capabilitiesMetaData.GetBool(CapabilityConstants.IsOnlyServerPagable), capabilitiesMetaData.GetArray(CapabilityConstants.ServerPagingOptions));
        }

        private static bool ParseRecordPermissionCapabilities(IDictionary<string, IOpenApiAny> capabilitiesMetaData)
        {
            return capabilitiesMetaData.GetBool(CapabilityConstants.SupportsRecordPermission);
        }
    }
}
