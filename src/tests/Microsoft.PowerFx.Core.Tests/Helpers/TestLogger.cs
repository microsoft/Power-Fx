// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Logging;

namespace Microsoft.PowerFx.Core.Tests.Helpers
{
    internal class TestLogger : IPowerFxLogger
    {
        internal event EventHandler<(string scenarioName, Guid scenarioInstance, string customDimensions)> StartScenarioTestEvent;

        internal event EventHandler<(string scenarioName, Guid scenarioInstance, string customDimensions)> FailScenarioTestEvent;

        internal event EventHandler<(string scenarioName, Guid scenarioInstance, string customDimensions)> EndScenarioTestEvent;

        internal event EventHandler<string> WarningTestEvent;

        internal event EventHandler<string> ErrorTestEvent;

        internal event EventHandler<(string message, Exception exception)> ExceptionTestEvent;

        internal event EventHandler<(string eventName, string customDimensions)> TrackTestEvent;        

        public void StartScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
        {
            StartScenarioTestEvent?.Invoke(this, (scenarioName, scenarioInstance, customDimensions));
        }

        public void FailScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
        {
            FailScenarioTestEvent?.Invoke(this, (scenarioName, scenarioInstance, customDimensions));
        }

        public void EndScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
        {
            EndScenarioTestEvent?.Invoke(this, (scenarioName, scenarioInstance, customDimensions));
        }

        public void Warning(string message)
        {
            WarningTestEvent?.Invoke(this, message);
        }

        public void Error(string message)
        {
            ErrorTestEvent?.Invoke(this, message);
        }

        public void Exception(Exception exception, string message)
        {
            ExceptionTestEvent?.Invoke(this, (message, exception));
        }

        public void Track(string eventName, string customDimensions)
        {
            TrackTestEvent?.Invoke(this, (eventName, customDimensions));
        }
    }
}
