// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Core.Entities;
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
    internal sealed class ServiceCapabilities : IColumnsCapabilities
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

        public static TableParameters ToTableParameters(ServiceCapabilities serviceCapabilities, string tableName, bool isReadOnly, ConnectorType connectorType, string datasetName)
        {
            FormulaType recordType = connectorType.FormulaType;

            SortRestrictions sortRestriction = new SortRestrictions()
            {
                AscendingOnlyProperties = serviceCapabilities?.SortRestriction?.AscendingOnlyProperties,
                UnsortableProperties = serviceCapabilities?.SortRestriction?.UnsortableProperties
            };
            FilterRestrictions filterRestriction = new FilterRestrictions()
            {
                RequiredProperties = serviceCapabilities?.FilterRestriction?.RequiredProperties,
                NonFilterableProperties = serviceCapabilities?.FilterRestriction?.NonFilterableProperties
            };
            SelectionRestrictions selectionRestriction = new SelectionRestrictions()
            {
                IsSelectable = serviceCapabilities?.SelectionRestriction?.IsSelectable ?? false
            };
            GroupRestrictions groupRestriction = new GroupRestrictions()
            {
                UngroupableProperties = serviceCapabilities?.GroupRestriction?.UngroupableProperties
            };
            Core.Entities.PagingCapabilities pagingCapabilities = new Core.Entities.PagingCapabilities()
            {
                IsOnlyServerPagable = serviceCapabilities?.PagingCapabilities?.IsOnlyServerPagable ?? false,
                ServerPagingOptions = serviceCapabilities?.PagingCapabilities?.ServerPagingOptions?.ToArray()
            };

            Dictionary<string, Core.Entities.ColumnCapabilitiesBase> columnCapabilities = serviceCapabilities?._columnsCapabilities?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value switch
                {
                    ColumnCapabilities cc => new Core.Entities.ColumnCapabilities(new Core.Entities.ColumnCapabilitiesDefinition() 
                    {
                        FilterFunctions = cc.Capabilities.FilterFunctions,
                        QueryAlias = cc.Capabilities.QueryAlias, 
                        IsChoice = cc.Capabilities.IsChoice
                    }) as Core.Entities.ColumnCapabilitiesBase,
                    ComplexColumnCapabilities ccc => new Core.Entities.ComplexColumnCapabilities() as Core.Entities.ColumnCapabilitiesBase,
                    _ => throw new NotImplementedException()
                });

            Dictionary<string, string> columnWithRelationships = connectorType.Fields.Where(f => f.ExternalTables?.Any() == true).Select(f => (f.Name, f.ExternalTables.First())).ToDictionary(tpl => tpl.Name, tpl => tpl.Item2);

            return new TableParameters()
            {
                TableName = tableName,
                IsReadOnly = isReadOnly,
                RecordType = recordType,
                DatasetName = datasetName,
                SortRestriction = sortRestriction,
                FilterRestriction = filterRestriction,
                SelectionRestriction = selectionRestriction,
                GroupRestriction = groupRestriction,
                FilterFunctions = serviceCapabilities?.FilterFunctions,
                FilterSupportedFunctions = serviceCapabilities?.FilterSupportedFunctions,
                PagingCapabilities = pagingCapabilities,
                SupportsRecordPermission = serviceCapabilities?.SupportsRecordPermission ?? false,
                SupportsDataverseOffline = serviceCapabilities?.SupportsDataverseOffline ?? false,
                ColumnsCapabilities = columnCapabilities,
                ColumnsWithRelationships = columnWithRelationships
            };
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _columnsCapabilities ??= new Dictionary<string, ColumnCapabilitiesBase>();
            _columnsCapabilities.Add(name, capability);
        }

        public void AddColumnCapabilities(IEnumerable<string> fieldNames = null)
        {
            foreach (string fieldName in fieldNames)
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
