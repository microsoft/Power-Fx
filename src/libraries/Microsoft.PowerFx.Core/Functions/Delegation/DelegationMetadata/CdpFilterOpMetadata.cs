// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal class CdpFilterOpMetadata : FilterOpMetadata
    {
        private readonly TableDelegationInfo _delegationInfo;

        public CdpFilterOpMetadata(AggregateType schema, TableDelegationInfo delegationInfo)
           : base(schema._type, null, null, GetFilterFunctionsSupportedByAllColumns(delegationInfo), GetFilterFunctionSupportedByTable(delegationInfo))
        {
            _delegationInfo = delegationInfo;
        }

        private static DelegationCapability GetFilterFunctionsSupportedByAllColumns(TableDelegationInfo delegationInfo)
        {
            DelegationCapability filterFunctionSupportedByAllColumns = DelegationCapability.None;

            if (delegationInfo?.FilterFunctions != null)
            {
                foreach (DelegationOperator globalFilterFunctionEnum in delegationInfo.FilterFunctions)
                {
                    string globalFilterFunction = globalFilterFunctionEnum.ToString().ToLowerInvariant();

                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalFilterFunction, out DelegationCapability globalFilterFunctionCapability))
                    {
                        filterFunctionSupportedByAllColumns |= globalFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            return filterFunctionSupportedByAllColumns;
        }

        private static DelegationCapability? GetFilterFunctionSupportedByTable(TableDelegationInfo delegationInfo)
        {
            DelegationCapability? filterFunctionsSupportedByTable = null;

            if (delegationInfo?.FilterSupportedFunctions != null)
            {
                filterFunctionsSupportedByTable = DelegationCapability.None;

                foreach (DelegationOperator globalSupportedFilterFunctionEnum in delegationInfo.FilterSupportedFunctions)
                {
                    string globalSupportedFilterFunction = globalSupportedFilterFunctionEnum.ToString().ToLowerInvariant();

                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalSupportedFilterFunction, out DelegationCapability globalSupportedFilterFunctionCapability))
                    {
                        filterFunctionsSupportedByTable |= globalSupportedFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            return filterFunctionsSupportedByTable;
        }

        public override bool TryGetColumnCapabilities(DPath columnPath, out DelegationCapability capabilities)
        {
            Contracts.AssertValid(columnPath);
            ColumnCapabilitiesDefinition columnCapabilityDefinition = _delegationInfo.GetColumnCapability(columnPath.Name.Value);

            if (columnCapabilityDefinition != null)
            {
                capabilities = columnCapabilityDefinition.ToDelegationCapability();
                return true;
            }

            capabilities = default;
            return false;
        }
    }
}
