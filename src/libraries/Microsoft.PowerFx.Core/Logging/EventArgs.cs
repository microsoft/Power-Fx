// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Logging
{
    internal interface ITrackEventArgs
    {
        string EventName { get; }
        string SerializedJson { get; }
    }

    internal interface IEndScenarioEventArgs
    {
        string ScenarioGuid { get; }
        string SerializedJson { get; }
    }

    internal sealed class TrackEventArgs : ITrackEventArgs
    {
        public string EventName { get; }
        public string SerializedJson { get; }

        internal TrackEventArgs(string eventName, string serializedJson)
        {
            Contracts.AssertNonEmpty(eventName);
            Contracts.AssertNonEmpty(serializedJson);

            EventName = eventName;
            SerializedJson = serializedJson;
        }
    }

    internal sealed class EndScenarioEventArgs : IEndScenarioEventArgs
    {
        public string ScenarioGuid { get; }
        public string SerializedJson { get; }

        internal EndScenarioEventArgs(string scenarioGuid, string serializedJson)
        {
            Contracts.AssertNonEmpty(scenarioGuid);
            Contracts.AssertNonEmpty(serializedJson);

            ScenarioGuid = scenarioGuid;
            SerializedJson = serializedJson;
        }
    }
}