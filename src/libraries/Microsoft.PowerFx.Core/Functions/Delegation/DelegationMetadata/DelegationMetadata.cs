// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    /// <summary>
    /// This represents a delegatable operation metadata about the imported delegatable CdpDataSourceInfo.
    /// </summary>
    internal sealed partial class DelegationMetadata : IDelegationMetadata
    {
        private readonly CompositeCapabilityMetadata _compositeMetadata;

        public DelegationMetadata(DType schema, string delegationMetadataJson)
        {
            Contracts.AssertValid(schema);

            var metadataParser = new DelegationMetadataParser();
            _compositeMetadata = metadataParser.Parse(delegationMetadataJson, schema);
            Contracts.AssertValue(_compositeMetadata);

            Schema = schema;
        }
        
        public Dictionary<DPath, DPath> ODataPathReplacementMap { get { return _compositeMetadata.ODataPathReplacementMap; } }

        public SortOpMetadata SortDelegationMetadata { get { return _compositeMetadata.SortDelegationMetadata; } }
        public FilterOpMetadata FilterDelegationMetadata { get { return _compositeMetadata.FilterDelegationMetadata; } }
        public GroupOpMetadata GroupDelegationMetadata { get { return _compositeMetadata.GroupDelegationMetadata; } }
        public DType Schema { get; }
        public DelegationCapability TableAttributes { get { return _compositeMetadata.TableAttributes; } }
        public DelegationCapability TableCapabilities { get { return _compositeMetadata.TableCapabilities; } }
    }
}
