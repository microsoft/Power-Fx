// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        public DelegationMetadata(DType schema, string delegationMetadataJson)
            : base(schema, new DelegationMetadataParser().Parse(delegationMetadataJson, schema))
        {
        }

        public DelegationMetadata(DType schema, List<OperationCapabilityMetadata> metadata)
            : base(schema, new CompositeCapabilityMetadata(schema, metadata))
        {
        }

        private sealed class CompositeMetaParser : MetaParser
        {
            private readonly List<MetaParser> _metaParsers;

            public CompositeMetaParser()
            {
                _metaParsers = new List<MetaParser>();
            }

            public override OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema)
            {
                Contracts.AssertValid(schema);

                var capabilities = new List<OperationCapabilityMetadata>();
                foreach (var parser in _metaParsers)
                {
                    var capabilityMetadata = parser.Parse(dataServiceCapabilitiesJsonObject, schema);
                    if (capabilityMetadata != null)
                    {
                        capabilities.Add(capabilityMetadata);
                    }
                }

                return new CompositeCapabilityMetadata(schema, capabilities);
            }

            public void AddMetaParser(MetaParser metaParser)
            {
                Contracts.AssertValue(metaParser);

                _metaParsers.Add(metaParser);
            }
        }
    }
}
