// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Logging
{
    internal sealed class EventHandlers
    {
        public event EventHandler<ITrackEventArgs> TrackEvent;

        public event EventHandler<ITrackEventArgs> ScenarioStartEvent;

        public event EventHandler<IEndScenarioEventArgs> ScenarioEndEvent;

        public event EventHandler<IEndScenarioEventArgs> FailScenarioEvent;

        internal void RaiseTrackEvent(string eventName, string serializedJson)
        {
            Contracts.AssertNonEmpty(eventName);
            Contracts.AssertNonEmpty(serializedJson);

            TrackEvent?.Invoke(this, new TrackEventArgs(eventName, serializedJson));
        }

        internal void RaiseStartScenario(string eventName, string serializedJson)
        {
            Contracts.AssertNonEmpty(eventName);
            Contracts.AssertNonEmpty(serializedJson);

            ScenarioStartEvent?.Invoke(this, new TrackEventArgs(eventName, serializedJson));
        }

        internal void RaiseEndScenario(string scenarioGuid, string serializedJson)
        {
            Contracts.AssertNonEmpty(scenarioGuid);
            Contracts.AssertNonEmpty(serializedJson);

            ScenarioEndEvent?.Invoke(this, new EndScenarioEventArgs(scenarioGuid, serializedJson));
        }

        internal void RaiseFailScenario(string scenarioGuid, string serializedJson)
        {
            Contracts.AssertNonEmpty(scenarioGuid);
            Contracts.AssertNonEmpty(serializedJson);

            FailScenarioEvent?.Invoke(this, new EndScenarioEventArgs(scenarioGuid, serializedJson));
        }
    }
}