// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    public class TableParameters
    {
        // Defines unsortable columns or columns only supporting ascending ordering
        public SortRestrictions SortRestriction { get; init; }

        // Defines columns that cannot be sorted and required properties
        public FilterRestrictions FilterRestriction { get; init; }

        // Used to indicate whether this table has selectable columns
        public SelectionRestrictions SelectionRestriction { get; init; }

        // Defines ungroupable columns
        public GroupRestrictions GroupRestriction { get; init; }

        // Filter functions supported by all columns
        public IEnumerable<string> FilterFunctions { get; init; }

        // Filter functions supported by table
        public IEnumerable<string> FilterSupportedFunctions { get; init; }

        // Defines paging capabilities
        internal PagingCapabilities PagingCapabilities { get; init; }

        // Defined per column capabilities
        public IReadOnlyCollection<KeyValuePair<string, ColumnCapabilitiesBase>> ColumnsCapabilities { get; init; }

        // Used for offline DV tables
        internal bool SupportsDataverseOffline { get; init; }

        // Supports per record permission
        internal bool SupportsRecordPermission { get; init; }

        // Name of table
        public string TableName { get; init; }

        // Read-Only
        public bool IsReadOnly { get; init; }

        // Dataset name
        public string DatasetName { get; init; }

        // Defines columns with relationships
        internal Dictionary<string, string> ColumnsWithRelationships { get; init; }

        public TableParameters()
        {
            PagingCapabilities = new PagingCapabilities()
            {
                IsOnlyServerPagable = false,
                ServerPagingOptions = new string[0]
            };
            SupportsDataverseOffline = false;
            SupportsRecordPermission = true;
            ColumnsWithRelationships = new Dictionary<string, string>();
        }

        public static TableParameters Default(string tableName, bool isReadOnly, string datasetName)
        {
            return new TableParameters()
            {
                TableName = tableName,
                IsReadOnly = isReadOnly,                
                DatasetName = datasetName,
                SortRestriction = new SortRestrictions()
                {
                    AscendingOnlyProperties = new List<string>(),
                    UnsortableProperties = new List<string>()
                },
                FilterRestriction = new FilterRestrictions()
                {
                    RequiredProperties = new List<string>(),
                    NonFilterableProperties = new List<string>()
                },
                SelectionRestriction = new SelectionRestrictions()
                {
                    IsSelectable = true
                },
                GroupRestriction = new GroupRestrictions()
                {
                    UngroupableProperties = new List<string>()
                },
                FilterFunctions = ColumnCapabilities.DefaultFilterFunctionSupport,
                FilterSupportedFunctions = ColumnCapabilities.DefaultFilterFunctionSupport                
            };
        }
    }

    internal sealed class ComplexColumnCapabilities : ColumnCapabilitiesBase
    {
        internal Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        public ComplexColumnCapabilities()
        {
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }
    }

    public sealed class ColumnCapabilities : ColumnCapabilitiesBase
    {
        public Dictionary<string, ColumnCapabilitiesBase> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        public ColumnCapabilitiesDefinition Capabilities;

        public static readonly IEnumerable<string> DefaultFilterFunctionSupport = new string[] { "eq", "ne", "gt", "ge", "lt", "le", "and", "or", "cdsin", "contains", "startswith", "endswith", "not", "null", "sum", "average", "min", "max", "count", "countdistinct", "top", "astype", "arraylookup" };

        public static ColumnCapabilities DefaultCdsColumnCapabilities => new ColumnCapabilities()
        {
            Capabilities = new ColumnCapabilitiesDefinition()
            {
                FilterFunctions = DefaultFilterFunctionSupport,
                QueryAlias = null,
                IsChoice = null
            },
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>()
        };

        private ColumnCapabilities()
        {
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }

        public ColumnCapabilities(ColumnCapabilitiesDefinition capability)
        {
            Contracts.AssertValueOrNull(capability);

            Capabilities = capability;
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }
    }

    public sealed class ColumnCapabilitiesDefinition
    {
        // ex: lt, le, eq, ne, gt, ge, and, or, not, contains, startswith, endswith, countdistinct, day, month, year, time
        public IEnumerable<string> FilterFunctions { get; init; }

        // used to rename column names
        // used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts&_a=contents&version=GBmaster
        public string QueryAlias { get; init; }

        // sharepoint delegation specific
        public bool? IsChoice { get; init; }

        public ColumnCapabilitiesDefinition()
        {
        }

        internal DelegationCapability ToDelegationCapability()
        {
            DelegationCapability columnDelegationCapability = DelegationCapability.None;

            foreach (string columnFilterFunction in FilterFunctions)
            {
                if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(columnFilterFunction, out DelegationCapability filterFunctionCapability))
                {
                    columnDelegationCapability |= filterFunctionCapability;
                }
            }

            columnDelegationCapability |= DelegationCapability.Filter;

            return columnDelegationCapability;
        }
    }

    public abstract class ColumnCapabilitiesBase
    {
    }

    public sealed class PagingCapabilities
    {
        public bool IsOnlyServerPagable { get; init; }

        public IEnumerable<string> ServerPagingOptions { get; init; }

        public PagingCapabilities()
        {
        }

        public PagingCapabilities(bool isOnlyServerPagable, string[] serverPagingOptions)
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

    public sealed class GroupRestrictions
    {
        public IList<string> UngroupableProperties { get; init; }

        public GroupRestrictions()
        {
        }
    }

    public sealed class SelectionRestrictions
    {
        // Indicates whether this table has selectable columns
        // Used in https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=/src/Cloud/DocumentServer.Core/Document/Document/InfoTypes/CdsDataSourceInfo.cs&_a=contents&version=GBmaster
        public bool IsSelectable { get; init; }

        public SelectionRestrictions()
        {
        }
    }

    public sealed class FilterRestrictions
    {
        // List of required properties
        public IList<string> RequiredProperties { get; init; }

        // List of non filterable properties
        public IList<string> NonFilterableProperties { get; init; }

        public FilterRestrictions()
        {
        }
    }

    public sealed class SortRestrictions
    {
        public IList<string> AscendingOnlyProperties { get; init; }

        public IList<string> UnsortableProperties { get; init; }

        public SortRestrictions()
        {
        }

        public SortRestrictions(IList<string> unsortableProperties, IList<string> ascendingOnlyProperties)
        {
            // List of properties which support ascending order only
            AscendingOnlyProperties = ascendingOnlyProperties;

            // List of unsortable properties
            UnsortableProperties = unsortableProperties;
        }
    }
}
