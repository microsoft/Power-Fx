// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        private abstract class MetaParser
        {
            public abstract OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema);

            protected DelegationCapability ParseColumnCapability(JsonElement columnCapabilityJsonObj, string capabilityKey)
            {
                Contracts.AssertNonEmpty(capabilityKey);

                // Retrieve the entry for the column using column name as key.
                if (!columnCapabilityJsonObj.TryGetProperty(capabilityKey, out var functionsJsonArray))
                {
                    return DelegationCapability.None;
                }

                DelegationCapability columnCapability = DelegationCapability.None;
                foreach (var op in functionsJsonArray.EnumerateArray())
                {
                    var operatorStr = op.GetString();
                    Contracts.AssertNonEmpty(operatorStr);

                    // If we don't support the operator then don't look at this capability.
                    if (!DelegationCapability.OperatorToDelegationCapabilityMap.ContainsKey(operatorStr))
                    {
                        continue;
                    }

                    columnCapability |= DelegationCapability.OperatorToDelegationCapabilityMap[operatorStr];
                }

                return columnCapability;
            }
        }
    }
}
