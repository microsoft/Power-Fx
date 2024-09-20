// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalTabularDataSource : IExternalDataSource, IDisplayMapped<string>
    {
        TabularDataQueryOptions QueryOptions { get; }

        /// <summary>
        /// Some data sources (like Dataverse) may return a cached value for
        /// the number of rows (calls to CountRows) instead of always retrieving
        /// the latest count.
        /// </summary>
        bool HasCachedCountRows { get; }

        IReadOnlyList<string> GetKeyColumns();

        IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo);

        bool CanIncludeSelect(string selectColumnName);

        bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName);

        bool CanIncludeExpand(IExpandInfo expandToAdd);

        bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd);
    }
}
