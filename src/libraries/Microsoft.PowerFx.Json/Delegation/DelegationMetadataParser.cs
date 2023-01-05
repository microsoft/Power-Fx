// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        private sealed class DelegationMetadataParser
        {
            public CompositeCapabilityMetadata Parse(string delegationMetadataJson, DType tableSchema)
            {
                Contracts.AssertValid(tableSchema);

                using var result = JsonDocument.Parse(delegationMetadataJson);

                var compositeParser = new CompositeMetaParser();
                compositeParser.AddMetaParser(new SortMetaParser());
                compositeParser.AddMetaParser(new FilterMetaParser());
                compositeParser.AddMetaParser(new GroupMetaParser());
                compositeParser.AddMetaParser(new ODataMetaParser());

                var dataServiceCapabilitiesJsonObject = result.RootElement;

                return compositeParser.Parse(dataServiceCapabilitiesJsonObject, tableSchema) as CompositeCapabilityMetadata;
            }
        }
    }
}
