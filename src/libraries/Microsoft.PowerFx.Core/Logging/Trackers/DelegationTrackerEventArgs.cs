// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    internal interface IDelegationTrackerEventArgs
    {
        DelegationStatus Status { get; }

        TexlNode Node { get; }

        TexlBinding Binding { get; }

        TexlFunction Func { get; }

        DelegationTelemetryInfo LogInfo { get; }
    }

    internal class DelegationTrackerEventArgs : IDelegationTrackerEventArgs
    {
        public DelegationStatus Status { get; }

        public TexlNode Node { get; }

        public TexlBinding Binding { get; }

        public TexlFunction Func { get; }

        public DelegationTelemetryInfo LogInfo { get; }

        public DelegationTrackerEventArgs(DelegationStatus status, TexlNode node,
            TexlBinding binding, TexlFunction func, DelegationTelemetryInfo logInfo = null)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValueOrNull(func);
            Contracts.AssertValueOrNull(logInfo);

            Status = status;
            Node = node;
            Binding = binding;
            Func = func;
            LogInfo = logInfo;
        }
    }
}
