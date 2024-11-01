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
           : base(schema._type, null, null, GetFilterFunctionsSupportedByAllColumns(delegationInfo), null)
        {
            _delegationInfo = delegationInfo;
        }

        private static DelegationCapability GetFilterFunctionsSupportedByAllColumns(TableDelegationInfo delegationInfo)
        {
            DelegationCapability filterFunctionSupportedByAllColumns = DelegationCapability.None;

            if (delegationInfo?.FilterSupportedFunctions != null)
            {
                foreach (DelegationOperator globalFilterFunctionEnum in delegationInfo.FilterSupportedFunctions)
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
