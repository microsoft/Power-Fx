// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    // Defines group operation metadata.
    internal sealed class GroupOpMetadata : OperationCapabilityMetadata
    {
        private readonly Dictionary<DPath, DelegationCapability> _columnRestrictions;

        public GroupOpMetadata(DType schema, Dictionary<DPath, DelegationCapability> columnRestrictions)
            : base(schema)
        {
            Contracts.AssertValue(columnRestrictions);

            _columnRestrictions = columnRestrictions;
        }

        protected override Dictionary<DPath, DelegationCapability> ColumnRestrictions => _columnRestrictions;

        public override DelegationCapability DefaultColumnCapabilities => DelegationCapability.Group;

        public override DelegationCapability TableCapabilities => DefaultColumnCapabilities;
    }
}
