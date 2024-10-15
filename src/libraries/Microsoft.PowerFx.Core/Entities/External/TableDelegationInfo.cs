﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    // Supports delegation information for CDP connectors
    public abstract class TableDelegationInfo
    {
        // Defines unsortable columns or columns only supporting ascending ordering
        public SortRestrictions SortRestriction { get; init; }

        // Defines columns that cannot be sorted and required properties
        public FilterRestrictions FilterRestriction { get; init; }

        // Used to indicate whether this table has selectable columns
        public SelectionRestrictions SelectionRestriction { get; init; }

        // Defines ungroupable columns
        public GroupRestrictions GroupRestriction { get; init; }

        // Filter functions supported by all columns of the table        
        public IEnumerable<DelegationOperator> FilterFunctions { get; init; }

        // Filter functions supported by the table
        public IEnumerable<DelegationOperator> FilterSupportedFunctions { get; init; }

        // Defines paging capabilities
        internal PagingCapabilities PagingCapabilities { get; init; }

        // Defining per column capabilities
        internal IReadOnlyCollection<KeyValuePair<string, ColumnCapabilitiesBase>> ColumnsCapabilities { get; init; }

        // Supports per record permission
        internal bool SupportsRecordPermission { get; init; }

        // Logical name of table
        public string TableName { get; init; }

        // Read-Only table
        public bool IsReadOnly { get; init; }

        // Dataset name
        public string DatasetName { get; init; }

        // Defines columns with relationships
        // Key = field logical name, Value = foreign table logical name
        internal Dictionary<string, string> ColumnsWithRelationships { get; init; }

        public virtual bool IsDelegable => (SortRestriction != null) || (FilterRestriction != null) || (FilterFunctions != null);

        public TableDelegationInfo()
        {
            PagingCapabilities = new PagingCapabilities()
            {
                IsOnlyServerPagable = false,
                ServerPagingOptions = new string[0]
            };
            SupportsRecordPermission = true;
            ColumnsWithRelationships = new Dictionary<string, string>();
        }

        public abstract ColumnCapabilitiesDefinition GetColumnCapability(string fieldName);
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
        public IReadOnlyDictionary<string, ColumnCapabilitiesBase> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        private ColumnCapabilitiesDefinition _capabilities;

        // Those are default CDS filter supported functions 
        // From // PowerApps-Client\src\Language\PowerFx.Dataverse.Parser\Importers\DataDescription\CdsCapabilities.cs
        public static readonly IEnumerable<DelegationOperator> DefaultFilterFunctionSupport = new DelegationOperator[]
        {
            DelegationOperator.And,
            DelegationOperator.Arraylookup,
            DelegationOperator.Astype,
            DelegationOperator.Average,
            DelegationOperator.Cdsin,
            DelegationOperator.Contains,
            DelegationOperator.Count,
            DelegationOperator.Countdistinct,
            DelegationOperator.Endswith,
            DelegationOperator.Eq,
            DelegationOperator.Ge,
            DelegationOperator.Gt,
            DelegationOperator.Le,
            DelegationOperator.Lt,
            DelegationOperator.Max,
            DelegationOperator.Min,
            DelegationOperator.Ne,
            DelegationOperator.Not,
            DelegationOperator.Null,
            DelegationOperator.Or,
            DelegationOperator.Startswith,
            DelegationOperator.Sum,
            DelegationOperator.Top
        };

        public static ColumnCapabilities DefaultColumnCapabilities => new ColumnCapabilities()
        {
            _capabilities = new ColumnCapabilitiesDefinition()
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

            _capabilities = capability;
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }
    }

    public sealed class ColumnCapabilitiesDefinition
    {
        public IEnumerable<DelegationOperator> FilterFunctions
        {
            get => _filterFunctions ?? ColumnCapabilities.DefaultFilterFunctionSupport;
            init => _filterFunctions = value;
        }

        // Used in PowerApps-Client/src/AppMagic/js/Core/Core.Data/ConnectedDataDeserialization/TabularDataDeserialization.ts
        // Used by SP connector only to rename column logical names in OData queries
        internal string QueryAlias { get; init; }

        // Sharepoint delegation specific
        internal bool? IsChoice { get; init; }

        private IEnumerable<DelegationOperator> _filterFunctions;
                
        public ColumnCapabilitiesDefinition()
        {
        }

        internal DelegationCapability ToDelegationCapability()
        {
            DelegationCapability columnDelegationCapability = DelegationCapability.None;

            foreach (DelegationOperator columnFilterFunctionEnum in FilterFunctions)
            {
                string columnFilterFunction = columnFilterFunctionEnum.ToString().ToLowerInvariant();

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

    internal sealed class PagingCapabilities
    {
        // Defines is the tabular connector is supporting client or server paging
        // If true, @odata.nextlink URL is used instead of $skip and $top query parameters
        // If false, $top and $skip will be used
        public bool IsOnlyServerPagable { get; init; }

        // Only supported values "top" and "skiptoken"
        // Used to define paging options to use 
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
        // Defines properties can cannot be grouped
        public IList<string> UngroupableProperties { get; init; }

        public GroupRestrictions()
        {
        }
    }

    public sealed class SelectionRestrictions
    {
        // Indicates whether this table has selectable columns ($select)
        // Columns with an Attachment will be excluded
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

        // List of non filterable properties (like images)
        public IList<string> NonFilterableProperties { get; init; }

        public FilterRestrictions()
        {
        }
    }

    public sealed class SortRestrictions
    {
        // Columns only supported ASC ordering
        public IList<string> AscendingOnlyProperties { get; init; }

        // Columns that don't support ordering
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
