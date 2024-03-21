// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalTabularDataSource : IExternalDataSource, IDisplayMapped<string>
    {
        TabularDataQueryOptions QueryOptions { get; }

        IReadOnlyList<string> GetKeyColumns();

        IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo);

        RecordValue GetDefaultRecord();

        bool CanIncludeSelect(string selectColumnName);

        bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName);

        bool CanIncludeExpand(IExpandInfo expandToAdd);

        bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd);
    }
}
