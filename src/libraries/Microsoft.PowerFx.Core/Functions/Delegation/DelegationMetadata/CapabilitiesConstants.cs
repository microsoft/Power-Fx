﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    // Service Capabilities Constants.
    internal static class CapabilitiesConstants
    {
        public const string Sort_UnsortableProperties = "unsortableProperties";
        public const string Sort_AscendingOnlyProperties = "ascendingOnlyProperties";

        // this constant should be "sortRestrictions" (extra s)
        public const string Sort_Restriction = "sortRestriction";

        // this constant should be "filterRestrictions" (extra s)
        public const string Filter_Restriction = "filterRestriction";
        public const string Filter_Functions = "filterFunctions";
        public const string Filter_SupportedFunctions = "filterFunctionSupport";
        public const string Filter_RequiredProperties = "requiredProperties";
        public const string Filter_NonFilterableProperties = "nonFilterableProperties";

        public const string Group_Restriction = "groupRestriction";
        public const string Group_UngroupableProperties = "ungroupableProperties";

        public const string Selection_Restriction = "selectRestrictions";
        public const string Selection_Selectable = "selectable";
        public const string ServerPagingOptions = "serverPagingOptions";
        public const string IsOnlyServerPagable = "isOnlyServerPagable";

        // this constant should be "isPageable" (extra e)
        public const string IsPagable = "isPagable";
        public const string IsDelegable = "isDelegable";
        public const string ColumnsCapabilities = "columnsCapabilities";
        public const string Capabilities = "capabilities";
        public const string PropertyQueryAlias = "queryAlias";
        public const string PropertyIsChoice = "isChoice";
        public const string Properties = "properties";
        public const string ODataVersion = "oDataVersion";
        public const string SupportsRecordPermission = "supportsRecordPermission";
    }
}
