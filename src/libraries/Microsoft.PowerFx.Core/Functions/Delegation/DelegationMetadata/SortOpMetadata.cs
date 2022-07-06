// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    // Defines sort operation metadata.
    internal sealed class SortOpMetadata : OperationCapabilityMetadata
    {
        private readonly Dictionary<DPath, DelegationCapability> _columnRestrictions;

        public SortOpMetadata(DType schema, Dictionary<DPath, DelegationCapability> columnRestrictions)
            : base(schema)
        {
            Contracts.AssertValue(columnRestrictions);

            _columnRestrictions = columnRestrictions;
        }

        protected override Dictionary<DPath, DelegationCapability> ColumnRestrictions => _columnRestrictions;

        public override DelegationCapability DefaultColumnCapabilities => DelegationCapability.Sort;

        public override DelegationCapability TableCapabilities => DefaultColumnCapabilities;

        // Returns true if column is marked as AscendingOnly.
        public bool IsColumnAscendingOnly(DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            if (!TryGetColumnRestrictions(columnPath, out var columnRestrictions))
            {
                return false;
            }

            return columnRestrictions.HasCapability(DelegationCapability.SortAscendingOnly);
        }
    }
}
