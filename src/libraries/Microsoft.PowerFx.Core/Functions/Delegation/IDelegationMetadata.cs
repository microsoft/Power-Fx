// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    internal interface IDelegationMetadata
    {
        DType Schema { get; }

        DelegationCapability TableAttributes { get; }

        DelegationCapability TableCapabilities { get; }

        SortOpMetadata SortDelegationMetadata { get; }

        FilterOpMetadata FilterDelegationMetadata { get; }

        GroupOpMetadata GroupDelegationMetadata { get; }

        Dictionary<DPath, DPath> ODataPathReplacementMap { get; }
    }
}
