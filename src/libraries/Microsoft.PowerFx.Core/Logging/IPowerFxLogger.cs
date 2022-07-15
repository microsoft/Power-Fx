// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerFx.Logging
{
    /// <summary>
    /// Interface used for hosts to collect Power Fx internal telemetry.
    /// Power Fx will ensure that anything logged via this interface
    /// is sanitized and does not contain user data.
    /// </summary>
    public interface IPowerFxLogger
    {
        void Track(string eventName, string customDimensions);

        void Warning(string message);

        void Exception(Exception exception, string message);

        void Error(string message);

        void StartScenario(string scenarioName, Guid scenarioInstance, string customDimensions);

        void EndScenario(string scenarioName, Guid scenarioInstance, string customDimensions);

        void FailScenario(string scenarioName, Guid scenarioInstance, string customDimensions);
    }
}
