// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// For use with <see cref="TableValue"/> and delegation.
    /// Describe delegation parameters such as filters, column selection, and sorting. 
    /// </summary>
    public abstract class DelegationParameters
    {
        /// <summary>
        /// When using OData with top level aggregation the field name to use to store the result. e.g. result of Sum(Employees, Salary).
        /// </summary>
        public const string ODataAggregationResultFieldName = "result";

        /// <summary>
        /// When using OData with $count=True, the result is returned in below field.
        /// </summary>
        public const string ODataCountFieldName = "@odata.count";

        /// <summary>
        /// When returning Records, OData puts them in this field.
        /// </summary>
        internal const string ODataResultFieldName = "value";

        // internal const string ODataCountFieldName = "count";

        /// <summary>
        /// Which features does this use - so we can determine if we support it. 
        /// </summary>
        public abstract DelegationParameterFeatures Features { get; }

        /// <summary>
        /// Expected type query needs to return.
        /// </summary>
        public abstract FormulaType ExpectedReturnType { get; }

        /// <summary>
        /// Throw if the parameters use features outside the feature list. 
        /// </summary>
        /// <param name="allowedFeatures"></param>
        /// <exception cref="InvalidOperationException">Thrown if this has uses features outside the allowedFeatures parameter.</exception>
        public void EnsureOnlyFeatures(DelegationParameterFeatures allowedFeatures)
        {
            var flags = this.Features;

            // Fail if params have extra features not supported in Odata. 
            var notAllowed = ~allowedFeatures;
            if ((notAllowed & flags) != 0)
            {
                throw new InvalidOperationException($"This operations only supports ({allowedFeatures:x}). This object has unsupported capabilities: ({flags:x}).");
            }
        }

        /// <summary>
        /// Returns the list of (column name, ascending/descending) where ascending=true.
        /// </summary>
        /// <returns></returns>
        public abstract IReadOnlyCollection<(string, bool)> GetOrderBy();

        public abstract string GetOdataFilter();

        /// <summary>
        /// Retrieves a collection of column names that needs to be retrieved from the Source, these are same column logical name of source. Empty collection means all columns are requested.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyCollection{T}"/> of strings containing the names of the columns. Returns an empty
        /// collection if no columns are specified.</returns>
        public virtual IReadOnlyCollection<string> GetColumns()
        {
            return new string[0];
        }

        /// <summary>
        /// Returns the list of (column name, alias) where alias is the name to use in the result, column name is the logical name in the source.
        /// </summary>
        /// <returns></returns>
        public virtual IReadOnlyCollection<(string, string)> GetColumnsWithAlias()
        {
            // Default implementation returns no columns with alias.
            return new (string, string)[0];
        }

        /// <summary>
        /// Get OData $apply parameter string.
        /// </summary>
        /// <returns></returns>
        public abstract string GetODataApply();

        /// <summary>
        /// Get OData $count flag.
        /// </summary>
        /// <returns></returns>
        public abstract bool ReturnTotalCount();

        /// <summary>
        /// Returns OData query string which has all parameter like $filter, $apply, etc.
        /// </summary>
        /// <returns></returns>
        public abstract string GetODataQueryString(QueryMarshallerSettings queryMarshallerSettings);

        public int? Top { get; set; }
    }

    public class QueryMarshallerSettings
    {
        /// <summary>
        /// OData boolean values are true/false. Sharepoint needs 1/0.
        /// </summary>
        public bool EncodeBooleanAsInteger { get; init; } = false;

        /// <summary>
        /// Gets a value indicating whether dates should be encoded as strings. Sharepoint needs this.
        /// </summary>
        public bool EncodeDateAsString { get; init; } = false;

        public QueryMarshallerSettings()
        {
        }
    }

    /// <summary>
    /// Let <see cref="DelegationParameters"/> describe which virtuals it implements. 
    /// This allows consumers to check for unsupported features. 
    /// </summary>
    [Flags]
    public enum DelegationParameterFeatures
    {
        // $filter
        Filter = 1 << 0,

        // $top
        Top = 1 << 1,

        // $select
        Columns = 1 << 2,

        // $orderBy
        Sort = 1 << 3,

        // $apply = join(table As name)
        ApplyJoin = 1 << 4,

        // $apply = groupby((field1, ..), field with sum as TotalSum)
        ApplyGroupBy = 1 << 5,

        // $count
        Count = 1 << 6,

        // $apply = aggregate(field1 with sum as TotalSum)
        ApplyTopLevelAggregation = 1 << 7,

        /*
          To be implemented later when needed
         
        // $compute
        Compute = 1 << 5,

        // $expand
        Expand = 1 << 7,

        // $format
        Format = 1 << 8,

        // $index
        Index = 1 << 9,

        // $levels
        Levels = 1 << 10,

        // $schemaversion
        SchemaVersion = 1 << 11,

        // $search
        Search = 1 << 12,

        // $skip
        Skip = 1 << 13

        */
    }
}
