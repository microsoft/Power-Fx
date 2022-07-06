// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    // Container type for all OperationCapabilityMetadata. This represents a metadata for the entire table.
    internal sealed class CompositeCapabilityMetadata : OperationCapabilityMetadata
    {
        private readonly List<OperationCapabilityMetadata> _compositeMetadata;

        public CompositeCapabilityMetadata(DType schema, List<OperationCapabilityMetadata> compositeMetadata)
            : base(schema)
        {
            Contracts.AssertValue(compositeMetadata);
            _compositeMetadata = compositeMetadata;
        }

        public Dictionary<DPath, DPath> ODataPathReplacementMap
        {
            get
            {
                var op = _compositeMetadata.OfType<ODataOpMetadata>().SingleOrDefault();
                return op != null ? op.QueryPathReplacement : new Dictionary<DPath, DPath>();
            }
        }

        public FilterOpMetadata FilterDelegationMetadata => _compositeMetadata.OfType<FilterOpMetadata>().SingleOrDefault();

        public SortOpMetadata SortDelegationMetadata => _compositeMetadata.OfType<SortOpMetadata>().SingleOrDefault();

        public GroupOpMetadata GroupDelegationMetadata => _compositeMetadata.OfType<GroupOpMetadata>().SingleOrDefault();

        public override DelegationCapability TableCapabilities
        {
            get
            {
                DelegationCapability capabilities = DelegationCapability.None;
                foreach (var metadata in _compositeMetadata)
                {
                    capabilities |= metadata.TableCapabilities;
                }

                return capabilities;
            }
        }

        public DelegationCapability TableAttributes
        {
            get
            {
                DelegationCapability capabilities = DelegationCapability.None;
                foreach (var metadata in _compositeMetadata)
                {
                    capabilities |= metadata.DefaultColumnCapabilities;
                }

                return capabilities;
            }
        }

        public override DelegationCapability DefaultColumnCapabilities
        {
            get
            {
                DelegationCapability capabilities = DelegationCapability.None;
                foreach (var metadata in _compositeMetadata)
                {
                    capabilities |= metadata.DefaultColumnCapabilities;
                }

                return capabilities;
            }
        }
    }
}
