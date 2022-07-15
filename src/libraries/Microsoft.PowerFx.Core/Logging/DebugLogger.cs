// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Logging
{
    public sealed partial class Log 
    {
        // Logger that logs to the debug console
        // Private inner class to ensure it's not used independent of the LoggingProvider
        private class DebugLogger : IPowerFxLogger
        {
            public void Track(string message, string customDimensions)
            {
                Log("Info", message);
            }

            public void Warning(string message)
            {
                Log("Warning", message);
            }
        
            public void Error(string message)
            {
                Log("Error", message);
            }

            public void Exception(Exception exception, string message)
            {
                Contracts.AssertValue(exception);
                Log("Exception", $"{message}\n{exception}");
            }

            public void StartScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
            {
                Log("StartScenario", $"{scenarioName}/{scenarioInstance}: {customDimensions}");
            }

            public void EndScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
            {
                Log("EndScenario", $"{scenarioName}/{scenarioInstance}: {customDimensions}");
            }

            public void FailScenario(string scenarioName, Guid scenarioInstance, string customDimensions)
            {
                Log("FailScenario", $"{scenarioName}/{scenarioInstance}): {customDimensions}");
            }

            private void Log(string type, string message)
            {
                Contracts.AssertValue(type);
                Contracts.AssertValue(message);

                Debug.WriteLine("[{0}] {1}", type, message);
            }
        }
    }
}
