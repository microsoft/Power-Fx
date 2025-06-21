// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    /// <summary>
    /// Supports delegation information for CDP connectors.
    /// </summary>
    public abstract class TableDelegationInfo
    {
        /// <summary>
        /// Defines unsortable columns or columns only supporting ascending ordering. If set to null, the table is not sortable.
        /// </summary>
        public SortRestrictions SortRestriction { get; init; }

        /// <summary>
        /// Defines columns that cannot be sorted and required properties.
        /// </summary>
        public FilterRestrictions FilterRestriction { get; init; }

        /// <summary>
        /// Used to indicate whether this table has selectable columns.
        /// </summary>
        public SelectionRestrictions SelectionRestriction { get; init; }

        [Obsolete("preview")]
        /// <summary>
        /// Gets the summarize capabilities for the table.
        /// </summary>
        public SummarizeCapabilities SummarizeCapabilities { get; init; }

        [Obsolete("preview")]
        /// <summary>
        /// Gets the count capabilities for the table.
        /// </summary>
        public CountCapabilities CountCapabilities { get; init; }

        [Obsolete("preview")]
        /// <summary>
        /// Gets the top level aggregation capabilities for the table.
        /// </summary>
        public TopLevelAggregationCapabilities TopLevelAggregationCapabilities { get; init; }

        /// <summary>
        /// Defines ungroupable columns.
        /// </summary>
        public GroupRestrictions GroupRestriction { get; init; }

        /// <summary>
        /// Filter functions supported by all columns of the table.
        /// </summary>
        public IEnumerable<DelegationOperator> FilterSupportedFunctions { get; init; }

        // Defines paging capabilities
        internal PagingCapabilities PagingCapabilities { get; init; }

        // Defining per column capabilities
        internal IReadOnlyDictionary<string, ColumnCapabilitiesBase> ColumnsCapabilities { get; init; }

        // Supports per record permission
        internal bool SupportsRecordPermission { get; init; }

        [Obsolete("preview")]
        /// <summary>
        /// Gets a value indicating whether the table supports join function.
        /// </summary>
        public bool SupportsJoinFunction { get; init; }

        /// <summary>
        /// Gets the logical name of the table.
        /// </summary>
        public string TableName { get; init; }

        /// <summary>
        /// Gets a value indicating whether the table is read-only.
        /// </summary>
        public bool IsReadOnly { get; init; }

        /// <summary>
        /// Gets a value indicating whether the table is sortable.
        /// </summary>
        public bool IsSortable => SortRestriction != null;

        /// <summary>
        /// Gets a value indicating whether columns can be selected.
        /// </summary>
        public bool IsSelectable => SelectionRestriction != null && SelectionRestriction.IsSelectable;

        /// <summary>
        /// Gets the dataset name.
        /// </summary>
        public string DatasetName { get; init; }

        /// <summary>
        /// Gets the primary key names. This array is ordered and supports multiple keys when composed key is used.
        /// </summary>
        public IEnumerable<string> PrimaryKeyNames { get; init; }

        // Defines columns with relationships
        // Key = field logical name, Value = foreign table logical name
        internal Dictionary<string, string> ColumnsWithRelationships { get; init; }

        /// <summary>
        /// Gets a value indicating whether the table is delegable.
        /// </summary>
        public virtual bool IsDelegable => IsSortable || (FilterRestriction != null) || (FilterSupportedFunctions != null);

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDelegationInfo"/> class.
        /// </summary>
        public TableDelegationInfo()
        {
            PagingCapabilities = new PagingCapabilities()
            {
                IsOnlyServerPagable = false,
                ServerPagingOptions = new ServerPagingOptions[0]
            };
            SupportsRecordPermission = true;
            ColumnsWithRelationships = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the column capability for the specified field name.
        /// </summary>
        /// <param name="fieldName">The logical name of the field.</param>
        /// <returns>The column capabilities definition for the specified field.</returns>
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

    /// <summary>
    /// Represents the capabilities of a column.
    /// </summary>
    public sealed class ColumnCapabilities : ColumnCapabilitiesBase
    {
        /// <summary>
        /// Gets the child column capabilities as a read-only dictionary, or null if none exist.
        /// </summary>
        public IReadOnlyDictionary<string, ColumnCapabilitiesBase> Properties => _childColumnsCapabilities.Any() ? _childColumnsCapabilities : null;

        private Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        private ColumnCapabilitiesDefinition _capabilities;

        /// <summary>
        /// Gets the column capabilities definition.
        /// </summary>
        public ColumnCapabilitiesDefinition Definition => _capabilities;

        /// <summary>
        /// The default CDS filter supported functions.
        /// </summary>
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

        /// <summary>
        /// Gets the default column capabilities.
        /// </summary>
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

        /// <summary>
        /// Adds a child column capability.
        /// </summary>
        /// <param name="name">The name of the child column.</param>
        /// <param name="capability">The capability to add.</param>
        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnCapabilities"/> class with the specified capability definition.
        /// </summary>
        /// <param name="capability">The column capabilities definition.</param>
        public ColumnCapabilities(ColumnCapabilitiesDefinition capability)
        {
            Contracts.AssertValueOrNull(capability);

            _capabilities = capability;
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }
    }

    /// <summary>
    /// Defines the capabilities for a column.
    /// </summary>
    public sealed class ColumnCapabilitiesDefinition
    {
        /// <summary>
        /// Gets the filter functions supported by the column.
        /// </summary>
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
                
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnCapabilitiesDefinition"/> class.
        /// </summary>
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

    /// <summary>
    /// Represents the base class for column capabilities.
    /// </summary>
    public abstract class ColumnCapabilitiesBase
    {
    }

    internal sealed class PagingCapabilities
    {
        // Defines is the tabular connector is supporting client or server paging
        // If true, @odata.nextlink URL is used instead of $skip and $top query parameters
        // If false, $top and $skip will be used
        public bool IsOnlyServerPagable { get; init; }
        
        // Used to define paging options to use 
        public IEnumerable<ServerPagingOptions> ServerPagingOptions { get; init; }

        public PagingCapabilities()
        {
        }

        public PagingCapabilities(bool isOnlyServerPagable, ServerPagingOptions[] serverPagingOptions)
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

    internal enum ServerPagingOptions
    {
        Unknown,
        Top,
        SkipToken
    }

    /// <summary>
    /// Defines the restrictions for grouping columns.
    /// </summary>
    public sealed class GroupRestrictions
    {
        /// <summary>
        /// Gets the list of properties that cannot be grouped.
        /// </summary>
        public IList<string> UngroupableProperties { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupRestrictions"/> class.
        /// </summary>
        public GroupRestrictions()
        {
        }
    }

    /// <summary>
    /// Defines the restrictions for selecting columns.
    /// </summary>
    public sealed class SelectionRestrictions
    {
        /// <summary>
        /// Gets a value indicating whether this table has selectable columns ($select). Columns with an Attachment will be excluded.
        /// </summary>
        public bool IsSelectable { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionRestrictions"/> class.
        /// </summary>
        public SelectionRestrictions()
        {
        }
    }

    /// <summary>
    /// If the table supports summarize, return true.
    /// e.g. Summarize(Table, ColumnName, Sum(ColumnName)).
    /// For top level aggregation Sum(Table, ColumnName), use <see cref="TopLevelAggregationCapabilities"/>.
    /// </summary>
    [Obsolete("preview")]
    public class SummarizeCapabilities
    {
        /// <summary>
        /// If the table property supports summarize, return true.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="method">The summarize method.</param>
        /// <returns>True if the property supports summarize; otherwise, false.</returns>
        public virtual bool IsSummarizableProperty(string propertyName, SummarizeMethod method)
        {
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SummarizeCapabilities"/> class.
        /// </summary>
        public SummarizeCapabilities()
        {
        }
    }

    [Obsolete("preview")]
    public enum SummarizeMethod
    {
        None,
        Sum,
        Average,
        Min,
        Max,
        Count,
        CountRows
    }

    /// <summary>
    /// Defines the count capabilities for a table.
    /// </summary>
    [Obsolete("preview")]
    public class CountCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CountCapabilities"/> class.
        /// </summary>
        public CountCapabilities()
        {
        }

        /// <summary>
        /// If the table is countable, return true. 
        /// Relevant expression: CountRows(Table).
        /// </summary>
        /// <returns>True if the table is countable; otherwise, false.</returns>
        public virtual bool IsCountableTable()
        {
            return false;
        }

        /// <summary>
        /// If the table is countable after filter, return true.
        /// Relevant expression: CountRows(Filter(Table, Condition)); / CountIf(Table, Condition).
        /// </summary>
        /// <returns>True if the table is countable after filter; otherwise, false.</returns>
        public virtual bool IsCountableAfterFilter()
        {
            return false;
        }

        /// <summary>
        /// If the table is countable after join, return true.
        /// Relevant expression: CountRows(Join(Table1, Table2, ...)).
        /// </summary>
        /// <returns>True if the table is countable after join; otherwise, false.</returns>
        public virtual bool IsCountableAfterJoin()
        {
            return false;
        }

        /// <summary>
        /// If the table is countable after summarize, return true.
        /// Relevant expression: CountRows(Summarize(Table, ...)).
        /// </summary>
        /// <returns>True if the table is countable after summarize; otherwise, false.</returns>
        public virtual bool IsCountableAfterSummarize()
        {
            return false;
        }
    }

    /// <summary>
    /// If the table supports top level aggregation for a column, return true.
    /// e.g. Sum, Average, Min, Max, Count without Summarize(). 
    /// e.g. expression: Sum(Table, ColumnName).
    /// For aggregation with grouping, Summarize(Table, ColumnName, Sum(ColumnName)), use <see cref="SummarizeCapabilities"/>.
    /// </summary>
    [Obsolete("preview")]
    public class TopLevelAggregationCapabilities
    {
        /// <summary>
        /// If the table supports top level aggregation for a column, return true.
        /// </summary>
        /// <param name="method">The summarize method.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if top level aggregation is supported; otherwise, false.</returns>
        public virtual bool IsTopLevelAggregationSupported(SummarizeMethod method, string propertyName)
        {
            return false;
        }
    }

    /// <summary>
    /// Defines the restrictions for filtering columns.
    /// </summary>
    public sealed class FilterRestrictions
    {
        /// <summary>
        /// Gets the list of required properties.
        /// </summary>
        public IList<string> RequiredProperties { get; init; }

        /// <summary>
        /// Gets the list of non-filterable properties (like images).
        /// </summary>
        public IList<string> NonFilterableProperties { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRestrictions"/> class.
        /// </summary>
        public FilterRestrictions()
        {
        }
    }

    /// <summary>
    /// Defines the restrictions for sorting columns.
    /// </summary>
    public sealed class SortRestrictions
    {
        /// <summary>
        /// Gets the list of columns that only support ascending ordering.
        /// </summary>
        public IList<string> AscendingOnlyProperties { get; init; }

        /// <summary>
        /// Gets the list of columns that do not support ordering.
        /// </summary>
        public IList<string> UnsortableProperties { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortRestrictions"/> class.
        /// </summary>
        public SortRestrictions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortRestrictions"/> class with the specified unsortable and ascending-only properties.
        /// </summary>
        /// <param name="unsortableProperties">The list of unsortable properties.</param>
        /// <param name="ascendingOnlyProperties">The list of properties that only support ascending ordering.</param>
        public SortRestrictions(IList<string> unsortableProperties, IList<string> ascendingOnlyProperties)
        {
            AscendingOnlyProperties = ascendingOnlyProperties;
            UnsortableProperties = unsortableProperties;
        }
    }
}
