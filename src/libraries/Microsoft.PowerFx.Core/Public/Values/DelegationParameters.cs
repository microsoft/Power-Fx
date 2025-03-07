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
        /// Which features does this use - so we can determine if we support it. 
        /// </summary>
        public abstract DelegationParameterFeatures Features { get; }

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

        // 0 columns means return all columns.
        public virtual IReadOnlyCollection<string> GetColumns()
        {
            return new string[0];
        }

        public int? Top { get; set; }
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
