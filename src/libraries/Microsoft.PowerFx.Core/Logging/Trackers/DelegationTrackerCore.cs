// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    // Helps migrate from DelegationTracker in DocumentServer.Core
    internal class DelegationTrackerCore
    {
        // Marks telemetry in the rule. Needed in canvas, not needed in PowerFx.Core
        // DelegationTracker.SetDelegationTrackerStatus(DelegationStatus.NotANumberArgType, callNode, binding, this, DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo());
        public static void SetDelegationTrackerStatus(DelegationStatus status, TexlNode node, TexlBinding binding, TexlFunction func, DelegationTelemetryInfo logInfo = null)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(func);
            Contracts.AssertValueOrNull(logInfo);

            // The rule need not exist on ControlInfo yet. This happens when we are attempting
            // to create a namemap in which case we try to bind the rule before adding it to the control.
            if (!TryGetCurrentRule(binding, out var rule))
            {
                return;
            }

            rule.SetDelegationTrackerStatus(node, status, logInfo ?? DelegationTelemetryInfo.CreateEmptyDelegationTelemetryInfo(), func);
        }

        internal static bool TryGetCurrentRule(TexlBinding binding, out IExternalRule rule)
        {
            Contracts.AssertValue(binding);

            rule = null;

            var entity = binding.NameResolver?.CurrentEntity;
            var currentProperty = binding.NameResolver?.CurrentProperty;
            if (entity == null || !currentProperty.HasValue || !currentProperty.Value.IsValid)
            {
                return false;
            }

            return entity.TryGetRule(currentProperty.Value, out rule);
        }
    }
}
