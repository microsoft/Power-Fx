// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    /// <summary>
    /// This represents a delegatable operation metadata about the imported delegatable CdpDataSourceInfo.
    /// </summary>
    internal partial class DelegationMetadataBase : IDelegationMetadata
    {
        private readonly CompositeCapabilityMetadata _compositeMetadata;

        public DelegationMetadataBase(DType schema, CompositeCapabilityMetadata compositeMetadata)
        {
            Contracts.AssertValid(schema);
            Contracts.AssertValue(compositeMetadata);

            _compositeMetadata = compositeMetadata;
            Schema = schema;
        }

        public Dictionary<DPath, DPath> ODataPathReplacementMap => _compositeMetadata.ODataPathReplacementMap;

        public SortOpMetadata SortDelegationMetadata => _compositeMetadata.SortDelegationMetadata;

        public FilterOpMetadata FilterDelegationMetadata => _compositeMetadata.FilterDelegationMetadata;

        public GroupOpMetadata GroupDelegationMetadata => _compositeMetadata.GroupDelegationMetadata;

        public DType Schema { get; }

        public DelegationCapability TableAttributes => _compositeMetadata.TableAttributes;

        public DelegationCapability TableCapabilities => _compositeMetadata.TableCapabilities;
    }
}
