// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

#pragma warning disable SA1117

namespace Microsoft.PowerFx.Core.Entities
{
    public class ServiceCapabilities2
    {
        public readonly SortRestriction2 SortRestriction;

        public readonly FilterRestriction2 FilterRestriction;

        public readonly SelectionRestriction2 SelectionRestriction;

        public readonly GroupRestriction2 GroupRestriction;

        public readonly IEnumerable<string> FilterFunctions;

        public readonly IEnumerable<string> FilterSupportedFunctions;

        public readonly PagingCapabilities2 PagingCapabilities;

        public IReadOnlyCollection<KeyValuePair<string, ColumnCapabilitiesBase2>> ColumnsCapabilities;

        public readonly bool SupportsDataverseOffline;

        public readonly bool SupportsRecordPermission;

        public readonly string TableName;

        public readonly bool IsReadOnly;

        public readonly FormulaType RecordType;

        public readonly string DatasetName;

        public readonly Dictionary<string, string> ColumnsWithRelationships;

        public ServiceCapabilities2(string tableName, bool isReadOnly, FormulaType recordType, string datasetName, SortRestriction2 sortRestriction, FilterRestriction2 filterRestriction, 
                                    SelectionRestriction2 selectionRestriction, GroupRestriction2 groupRestriction, IEnumerable<string> filterFunctions, IEnumerable<string> filterSupportedFunctions, 
                                    PagingCapabilities2 pagingCapabilities, bool recordPermissionCapabilities, bool supportsDataverseOffline, Dictionary<string, ColumnCapabilitiesBase2> columnCapabilities,
                                    Dictionary<string, string> columnWithRelationships)
        {
            TableName = tableName;
            IsReadOnly = isReadOnly;
            RecordType = recordType;
            DatasetName = datasetName;
            SortRestriction = sortRestriction;
            FilterRestriction = filterRestriction;
            FilterFunctions = filterFunctions;
            PagingCapabilities = pagingCapabilities;
            SelectionRestriction = selectionRestriction;
            GroupRestriction = groupRestriction;
            SupportsDataverseOffline = supportsDataverseOffline;
            FilterSupportedFunctions = filterSupportedFunctions;
            ColumnsCapabilities = columnCapabilities;
            SupportsRecordPermission = recordPermissionCapabilities;
            ColumnsWithRelationships = columnWithRelationships;
        }

        public static ServiceCapabilities2 Default(string tableName, bool isReadOnly, FormulaType recordType, string datasetName, IEnumerable<string> fieldNames)
        {
            return new ServiceCapabilities2(
                tableName,
                isReadOnly,
                recordType,
                datasetName,
                new SortRestriction2(new List<string>() /* unsortableProperties */, new List<string>() /* ascendingOnlyProperties */),
                new FilterRestriction2(new List<string>() /* requiredProperties */, new List<string>() /* nonFilterableProperties */),
                new SelectionRestriction2(true /* isSelectable */),
                new GroupRestriction2(new List<string>() /* ungroupableProperties */),
                ColumnCapabilities2.DefaultFilterFunctionSupport, // filterFunctions
                ColumnCapabilities2.DefaultFilterFunctionSupport, // filterSupportedFunctions
                new PagingCapabilities2(false /* isOnlyServerPagable */, new string[0] /* serverPagingOptions */),
                true, /* recordPermissionCapabilities */
                false, /* supportsDataverseOffline */
                fieldNames.Select(f => new KeyValuePair<string, ColumnCapabilitiesBase2>(f, ColumnCapabilities2.DefaultCdsColumnCapabilities)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                null /* no relationship */);
        }
    }

    internal sealed class ComplexColumnCapabilities2 : ColumnCapabilitiesBase2
    {
        internal Dictionary<string, ColumnCapabilitiesBase2> _childColumnsCapabilities;

        public ComplexColumnCapabilities2()
        {
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase2>();
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase2 capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }
    }

    public sealed class ColumnCapabilities2 : ColumnCapabilitiesBase2
    {
        public Dictionary<string, ColumnCapabilitiesBase2> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private Dictionary<string, ColumnCapabilitiesBase2> _childColumnsCapabilities;

        public ColumnCapabilitiesDefinition2 Capabilities;

        public static readonly IEnumerable<string> DefaultFilterFunctionSupport = new string[] { "eq", "ne", "gt", "ge", "lt", "le", "and", "or", "cdsin", "contains", "startswith", "endswith", "not", "null", "sum", "average", "min", "max", "count", "countdistinct", "top", "astype", "arraylookup" };

        public static ColumnCapabilities2 DefaultCdsColumnCapabilities => new ColumnCapabilities2()
        {
            Capabilities = new ColumnCapabilitiesDefinition2(DefaultFilterFunctionSupport, null, null),
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase2>()
        };

        private ColumnCapabilities2()
        {
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase2 capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }

        public ColumnCapabilities2(ColumnCapabilitiesDefinition2 capability)
        {
            Contracts.AssertValueOrNull(capability);

            Capabilities = capability;
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase2>();
        }
    }

    public sealed class ColumnCapabilitiesDefinition2
    {
        public readonly IEnumerable<string> FilterFunctions;

        public readonly string QueryAlias;

        public readonly bool? IsChoice;

        public ColumnCapabilitiesDefinition2(IEnumerable<string> filterFunction, string alias, bool? isChoice)
        {
            // ex: lt, le, eq, ne, gt, ge, and, or, not, contains, startswith, endswith, countdistinct, day, month, year, time
            FilterFunctions = filterFunction;

            // used to rename column names
            // used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts&_a=contents&version=GBmaster
            QueryAlias = alias;

            // sharepoint delegation specific
            IsChoice = isChoice;
        }
    }

    public abstract class ColumnCapabilitiesBase2
    {
    }

    public sealed class PagingCapabilities2
    {
        public readonly bool IsOnlyServerPagable;

        public readonly IEnumerable<string> ServerPagingOptions;

        public PagingCapabilities2(bool isOnlyServerPagable, string[] serverPagingOptions)
        {
            // Server paging restrictions, true for CDS            
            // Setting 'IsOnlyServerPagable' to true in the table metadata response lets PowerApps application to use
            // @odata.nextlink URL in reponse message (instead of $skip and $top query parameters) for page traversal.
            // It is also required to set sortable and filterable restrictions for PowerApps to page through results.
            IsOnlyServerPagable = isOnlyServerPagable;

            // List of supported server-driven paging capabilities, null for CDS
            // ex: top, skiptoken
            // used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/AppMagic/js/AppMagic.Services/ConnectedData/CdpConnector.ts&_a=contents&version=GBmaster
            ServerPagingOptions = serverPagingOptions;
        }
    }

    public sealed class GroupRestriction2
    {
        public readonly IList<string> UngroupableProperties;

        public GroupRestriction2(IList<string> ungroupableProperties)
        {
            Contracts.AssertValueOrNull(ungroupableProperties);

            UngroupableProperties = ungroupableProperties;
        }
    }

    public sealed class SelectionRestriction2
    {
        public readonly bool IsSelectable;

        public SelectionRestriction2(bool isSelectable)
        {
            // Indicates whether this table has selectable columns
            // Used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/Cloud/DocumentServer.Core/Document/Document/InfoTypes/CdsDataSourceInfo.cs&_a=contents&version=GBmaster
            IsSelectable = isSelectable;
        }
    }

    public sealed class FilterRestriction2
    {
        public readonly IList<string> RequiredProperties;

        public readonly IList<string> NonFilterableProperties;

        public FilterRestriction2(IList<string> requiredProperties, IList<string> nonFilterableProperties)
        {
            // List of required properties
            RequiredProperties = requiredProperties;

            // List of non filterable properties
            NonFilterableProperties = nonFilterableProperties;
        }
    }

    public sealed class SortRestriction2
    {
        public readonly IList<string> AscendingOnlyProperties;

        public readonly IList<string> UnsortableProperties;

        public SortRestriction2(IList<string> unsortableProperties, IList<string> ascendingOnlyProperties)
        {
            // List of properties which support ascending order only
            AscendingOnlyProperties = ascendingOnlyProperties;

            // List of unsortable properties
            UnsortableProperties = unsortableProperties;
        }
    }
}
