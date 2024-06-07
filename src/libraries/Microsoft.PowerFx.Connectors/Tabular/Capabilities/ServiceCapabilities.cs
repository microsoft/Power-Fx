// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.PowerFx.Connectors.Tabular.Capabilities;
using Microsoft.PowerFx.Core.Utils;

// DO NOT INCLUDE Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata ASSEMBLY
// as it defines CapabilitiesConstants which has invalid values.

#pragma warning disable SA1117 // Parameters should be on separate lines

// <summary>
// Source code copied from PowerApps-Client repo
// src/Language/PowerFx.Dataverse.Parser/Importers/DataDescription/ServiceCapabilities.cs
// </summary>

namespace Microsoft.PowerFx.Connectors.Tabular
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
        public readonly string[] FilterFunctions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.FilterFunctionSupport)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public readonly string[] FilterSupportedFunctions;

        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.ServerPagingOptions)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] ServerPagingOptions => PagingCapabilities.ServerPagingOptions;

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

        public ServiceCapabilities(SortRestriction sortRestriction, FilterRestriction filterRestriction, SelectionRestriction selectionRestriction, GroupRestriction groupRestriction, string[] filterFunctions, string[] filterSupportedFunctions,
                                   PagingCapabilities pagingCapabilities, bool recordPermissionCapabilities, int oDataVersion = CurrentODataVersion, bool supportsDataverseOffline = false)
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

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            if (_columnsCapabilities == null)
            {
                _columnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
            }

            _columnsCapabilities.Add(name, capability);
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
