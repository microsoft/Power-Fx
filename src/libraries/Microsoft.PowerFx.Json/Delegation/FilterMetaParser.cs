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
        private sealed class FilterMetaParser : MetaParser
        {
            public override OperationCapabilityMetadata Parse(JsonElement dataServiceCapabilitiesJsonObject, DType schema)
            {
                Contracts.AssertValid(schema);

                // Check if any filter metadata is specified or not.
                var filterRestrictionExists = dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.Filter_Restriction, out var filterRestrictionJsonObject);
                var globalFilterFunctionsExists = dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.Filter_Functions, out var globalFilterFunctionsJsonArray);
                var globalFilterSupportsExists = dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.Filter_SupportedFunctions, out var globalFilterSupportedFunctionsJsonArray);
                var columnCapabilitiesExists = dataServiceCapabilitiesJsonObject.TryGetProperty(CapabilitiesConstants.ColumnsCapabilities, out var columnCapabilitiesJsonObj);

                if (!filterRestrictionExists && !globalFilterFunctionsExists && !globalFilterSupportsExists && !columnCapabilitiesExists)
                {
                    return null;
                }

                // Go through all filter restrictions if defined.
                var columnRestrictions = new Dictionary<DPath, DelegationCapability>();

                // If any nonFilterablepropertis exist then mark each column as such.
                if (filterRestrictionExists && filterRestrictionJsonObject.TryGetProperty(CapabilitiesConstants.Filter_NonFilterableProperties, out var nonFilterablePropertiesJsonArray))
                {
                    foreach (var prop in nonFilterablePropertiesJsonArray.EnumerateArray())
                    {
                        var columnName = DPath.Root.Append(new DName(prop.GetString()));
                        if (!columnRestrictions.ContainsKey(columnName))
                        {
                            columnRestrictions.Add(columnName, new DelegationCapability(DelegationCapability.Filter));
                        }
                    }
                }

                // Check for any FilterFunctions defined at table level.
                DelegationCapability filterFunctionsSupportedByAllColumns = DelegationCapability.None;
                if (globalFilterFunctionsExists)
                {
                    foreach (var op in globalFilterFunctionsJsonArray.EnumerateArray())
                    {
                        var operatorStr = op.GetString();
                        Contracts.AssertNonEmpty(operatorStr);

                        // If we don't support the operator then don't look at this capability.
                        if (!DelegationCapability.OperatorToDelegationCapabilityMap.ContainsKey(operatorStr))
                        {
                            continue;
                        }

                        // If filter functions are specified at table level then that means filter operation is supported.
                        filterFunctionsSupportedByAllColumns |= DelegationCapability.OperatorToDelegationCapabilityMap[operatorStr] | DelegationCapability.Filter;
                    }
                }

                // Check for any FilterSupportedFunctions defined at table level.
                DelegationCapability? filterFunctionsSupportedByTable = null;
                if (globalFilterSupportsExists)
                {
                    filterFunctionsSupportedByTable = DelegationCapability.None;
                    foreach (var op in globalFilterSupportedFunctionsJsonArray.EnumerateArray())
                    {
                        var operatorStr = op.GetString();
                        Contracts.AssertNonEmpty(operatorStr);

                        // If we don't support the operator then don't look at this capability.
                        if (!DelegationCapability.OperatorToDelegationCapabilityMap.ContainsKey(operatorStr))
                        {
                            continue;
                        }

                        // If filter functions are specified at table level then that means filter operation is supported.
                        filterFunctionsSupportedByTable |= DelegationCapability.OperatorToDelegationCapabilityMap[operatorStr] | DelegationCapability.Filter;
                    }
                }

                var columnCapabilities = new Dictionary<DPath, DelegationCapability>();
                if (!columnCapabilitiesExists)
                {
                    return new FilterOpMetadata(schema, columnRestrictions, columnCapabilities, filterFunctionsSupportedByAllColumns, filterFunctionsSupportedByTable);
                }

                // Sweep through all column filter capabilities.
                foreach (var column in columnCapabilitiesJsonObj.EnumerateObject())
                {
                    var columnPath = DPath.Root.Append(new DName(column.Name));

                    // Internal columns don't appear in schema and we don't gather any information about it as they don't appear in expressions.
                    // Task 790576: Runtime should provide visibility information along with delegation metadata information per column
                    if (!schema.Contains(columnPath))
                    {
                        continue;
                    }

                    // Get capabilities object for column
                    var capabilitiesDefinedByColumn = column.Value;

                    // Get properties object for the column
                    if (capabilitiesDefinedByColumn.TryGetProperty(CapabilitiesConstants.Properties, out var propertyCapabilities))
                    {
                        foreach (var property in propertyCapabilities.EnumerateObject())
                        {
                            var propertyPath = columnPath.Append(new DName(property.Name));
                            var capabilitiesDefinedByColumnProperty = property.Value;

                            if (!capabilitiesDefinedByColumnProperty.TryGetProperty(CapabilitiesConstants.Capabilities, out var propertyCapabilityJsonObject))
                            {
                                continue;
                            }

                            var propertyCapability = ParseColumnCapability(propertyCapabilityJsonObject, capabilityKey: CapabilitiesConstants.Filter_Functions);
                            if (propertyCapability.Capabilities != DelegationCapability.None)
                            {
                                Contracts.Assert(schema.Contains(propertyPath));

                                // If column is specified as non-filterable then this metadata shouldn't be present. 
                                // But if it is present then we should ignore it.
                                if (!columnRestrictions.ContainsKey(propertyPath))
                                {
                                    columnCapabilities.Add(propertyPath, propertyCapability | DelegationCapability.Filter);
                                }
                            }
                        }
                    }

                    // Get capability object defined for column. 
                    // This is optional as for columns with complex types (nested table or record), it will have "properties" key instead.
                    // We are not supporting that case for now. So we ignore it currently.
                    if (!capabilitiesDefinedByColumn.TryGetProperty(CapabilitiesConstants.Capabilities, out var capabilityJsonObject))
                    {
                        continue;
                    }

                    var isChoice = capabilityJsonObject.TryGetProperty(CapabilitiesConstants.PropertyIsChoice, out var isChoiceElement) && isChoiceElement.GetBoolean();

                    var capability = ParseColumnCapability(capabilityJsonObject, capabilityKey: CapabilitiesConstants.Filter_Functions);
                    if (capability.Capabilities != DelegationCapability.None)
                    {
                        Contracts.Assert(schema.Contains(columnPath));

                        // If column is specified as non-filterable then this metadata shouldn't be present. 
                        // But if it is present then we should ignore it.
                        if (!columnRestrictions.ContainsKey(columnPath))
                        {
                            columnCapabilities.Add(columnPath, capability | DelegationCapability.Filter);
                        }

                        if (isChoice == true)
                        {
                            var choicePropertyPath = columnPath.Append(new DName(ODataMetaParser.ValueProperty));
                            if (!columnRestrictions.ContainsKey(choicePropertyPath))
                            {
                                columnCapabilities.Add(choicePropertyPath, capability | DelegationCapability.Filter);
                            }
                        }
                    }
                }

                return new FilterOpMetadata(schema, columnRestrictions, columnCapabilities, filterFunctionsSupportedByAllColumns, filterFunctionsSupportedByTable);
            }
        }
    }
}
