// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        private sealed class GroupMetaParser : MetaParser
        {
            public override OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema)
            {
                Contracts.AssertValid(schema);

                var columnRestrictions = new Dictionary<DPath, DelegationCapability>();
                if (!dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.Group_Restriction, out var groupRestrictionJsonObject))
                {
                    return null;
                }

                if (groupRestrictionJsonObject.TryGetProperty(CapabilitiesConstants.Group_UngroupableProperties, out var ungroupablePropertiesJsonArray))
                {
                    foreach (var prop in ungroupablePropertiesJsonArray.EnumerateArray())
                    {
                        var columnName = new DName(prop.GetString());
                        var columnPath = DPath.Root.Append(columnName);

                        if (!columnRestrictions.ContainsKey(columnPath))
                        {
                            columnRestrictions.Add(columnPath, new DelegationCapability(DelegationCapability.Group));
                        }
                    }
                }

                return new GroupOpMetadata(schema, columnRestrictions);
            }
        }
    }
}
