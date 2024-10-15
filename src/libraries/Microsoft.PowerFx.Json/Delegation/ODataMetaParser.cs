﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    internal sealed partial class DelegationMetadata : DelegationMetadataBase
    {
        private sealed class ODataMetaParser : MetaParser
        {
            internal const string ValueProperty = "Value";

            public override OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema)
            {
                Contracts.AssertValid(schema);

                var oDataReplacement = new Dictionary<DPath, DPath>();

                if (dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.ColumnsCapabilities, out var columnCapabilitiesJsonObj))
                {
                    foreach (var column in columnCapabilitiesJsonObj.EnumerateObject())
                    {
                        var columnPath = DPath.Root.Append(new DName(column.Name));

                        var capabilitiesDefinedByColumn = column.Value;

                        if (capabilitiesDefinedByColumn.TryGetProperty(CapabilitiesConstants.Capabilities, out var columnCapabilities))
                        {
                            if (columnCapabilities.TryGetProperty(CapabilitiesConstants.PropertyIsChoice, out var choice) &&
                                choice.GetBoolean())
                            {
                                oDataReplacement.Add(columnPath.Append(new DName(ValueProperty)), columnPath);
                            }
                        }

                        if (!capabilitiesDefinedByColumn.TryGetProperty(CapabilitiesConstants.Properties, out var propertyCapabilities))
                        {
                            continue;
                        }

                        foreach (var property in propertyCapabilities.EnumerateObject())
                        {
                            var propertyPath = columnPath.Append(new DName(property.Name));
                            var capabilitiesDefinedByColumnProperty = property.Value;

                            if (!capabilitiesDefinedByColumnProperty.TryGetProperty(CapabilitiesConstants.Capabilities, out var propertyCapabilityJsonObject))
                            {
                                continue;
                            }

                            if (propertyCapabilityJsonObject.TryGetProperty(CapabilitiesConstants.PropertyQueryAlias, out var alias))
                            {
                                oDataReplacement.Add(propertyPath, Entities.DataSourceInfo.GetReplacementPath(alias.GetString(), columnPath));
                            }
                        }
                    }
                }

                return new ODataOpMetadata(schema, oDataReplacement);
            }
        }        
    }
}
