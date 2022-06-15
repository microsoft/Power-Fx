// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalRule
    {
        Dictionary<int, DataSourceToQueryOptionsMap> TexlNodeQueryOptions { get; }

        IExternalDocument Document { get; }

        TexlBinding Binding { get; }

        bool HasErrors { get; }
    }
}
