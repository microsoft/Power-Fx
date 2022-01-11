// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    internal class TrackingProvider
    {
        public static readonly TrackingProvider Instance = new TrackingProvider();

        internal event EventHandler<IAddSuggestionMessageEventArgs> AddSuggestionEvent;

        internal event EventHandler<IDelegationTrackerEventArgs> DelegationTrackerEvent;

        internal void AddSuggestionMessage(string message, TexlNode node, TexlBinding binding)
        {
            AddSuggestionEvent?.Invoke(this, new AddSuggestionMessageEventArgs(message, node, binding));
        }

        internal void SetDelegationTrackerStatus(
            DelegationStatus status,
            TexlNode node,
            TexlBinding binding,
            TexlFunction func,
            DelegationTelemetryInfo logInfo = null)
        {
            DelegationTrackerEvent?.Invoke(this, new DelegationTrackerEventArgs(status, node, binding, func, logInfo));
        }
    }
}
