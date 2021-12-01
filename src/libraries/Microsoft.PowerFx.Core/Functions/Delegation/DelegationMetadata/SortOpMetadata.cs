﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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

        protected override Dictionary<DPath, DelegationCapability> ColumnRestrictions { get { return _columnRestrictions; } }

        public override DelegationCapability DefaultColumnCapabilities { get { return DelegationCapability.Sort; } }

        public override DelegationCapability TableCapabilities { get { return DefaultColumnCapabilities; } }

        // Returns true if column is marked as AscendingOnly.
        public bool IsColumnAscendingOnly(DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            DelegationCapability columnRestrictions;
            if (!TryGetColumnRestrictions(columnPath, out columnRestrictions))
                return false;

            return columnRestrictions.HasCapability(DelegationCapability.SortAscendingOnly);
        }
    }
}
