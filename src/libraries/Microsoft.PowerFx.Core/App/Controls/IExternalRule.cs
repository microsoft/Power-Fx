// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalRule
    {
        Dictionary<int, DataSourceToQueryOptionsMap> TexlNodeQueryOptions { get; }

        IExternalDocument Document { get; }

        TexlBinding Binding { get; }

        bool HasErrors { get; }

        bool IsAsync { get; }

        bool HasControlPropertyDependency(string referencedControlUniqueId);

        void SetDelegationTrackerStatus(TexlNode node, DelegationStatus status, DelegationTelemetryInfo logInfo, TexlFunction func);
    }
}
