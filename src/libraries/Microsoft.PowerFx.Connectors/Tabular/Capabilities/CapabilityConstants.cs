// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors.Tabular.Capabilities
{
    // Those constants are also defined in Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata.CapabilitiesConstants
    // but some of them are invalid
    // - filterrestrictions (missing s)
    // - sortrestrictions (missing s)
    internal static class CapabilityConstants
    {
        public const string AscendingOnlyProperties = "ascendingOnlyProperties";
        public const string ColumnProperties = "properties";        
        public const string ColumnsCapabilities = "columnsCapabilities";
        public const string Filterable = "filterable";
        public const string FilterFunctions = "filterFunctions";
        public const string FilterFunctionSupport = "filterFunctionSupport";
        public const string FilterRequiredProperties = "requiredProperties";
        public const string FilterRestrictions = "filterRestrictions";
        public const string GroupRestriction = "groupRestriction";
        public const string IsDelegable = "isDelegable";
        public const string IsOnlyServerPagable = "isOnlyServerPagable";
        public const string IsPageable = "isPageable";
        public const string NonFilterableProperties = "nonFilterableProperties";
        public const string ODataVersion = "oDataVersion";
        public const string ODataversionOption = "odataVersion";
        public const string PropertyQueryAlias = "queryAlias";
        public const string Selectable = "selectable";
        public const string SelectionRestriction = "selectRestrictions";
        public const string SelectRestriction = "selectRestrictions";
        public const string ServerPagingOptions = "serverPagingOptions";
        public const string Sortable = "sortable";
        public const string SortRestrictions = "sortRestrictions";
        public const string SPDelegationSupport = "x-ms-sp";
        public const string SPIsChoice = "IsChoice";
        public const string SPQueryName = "OdataQueryName";
        public const string SupportsRecordPermission = "supportsRecordPermission";
        public const string UngroupableProperties = "ungroupableProperties";
        public const string UnsortableProperties = "unsortableProperties";
    }
}
