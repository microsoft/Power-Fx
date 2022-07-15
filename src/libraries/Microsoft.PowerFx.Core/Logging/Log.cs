// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Logging
{
    /// <summary>
    /// Provides a logging instance used by PowerFx. 
    /// Default Logs to console in DEBUG, but hosts can register loggers to also collect PowerFx telemetry.
    /// </summary>
    public sealed partial class Log
    {
        private IPowerFxLogger _logger;

        private static Log _instance;

        internal static Log Instance => _instance ??= new Log();

        private Log()
        {
#if DEBUG
            _logger = new DebugLogger();
#endif
        }

        internal void Track(string eventName, object customDimensions = null)
        {
            var serialized = customDimensions != null ? JsonSerializer.Serialize(customDimensions) : string.Empty;
            _logger?.Track(eventName, serialized);
        }

        internal void Warning(string message)
        {
            _logger?.Warning(message);
        }

        internal void Exception(Exception exception, string message = "")
        {
            _logger?.Exception(exception, message);
        }

        internal void Error(string message)
        {
            _logger.Error(message);
        }

        internal LoggingScenario StartScenario(string scenarioName, object customDimensions = null)
        {
            var serialized = customDimensions != null ? JsonSerializer.Serialize(customDimensions) : string.Empty;
            var scenario = new LoggingScenario(scenarioName);
            _logger?.StartScenario(scenario.ScenarioName, scenario.ScenarioInstance, serialized);
            return scenario;
        }

        internal void EndScenario(LoggingScenario scenario, object customDimensions = null)
        {
            scenario.CloseScenario();
            var serialized = customDimensions != null ? JsonSerializer.Serialize(customDimensions) : string.Empty;
            _logger?.EndScenario(scenario.ScenarioName, scenario.ScenarioInstance, serialized);
        }

        internal void FailScenario(LoggingScenario scenario, object customDimensions = null)
        {
            scenario.CloseScenario();
            var serialized = customDimensions != null ? JsonSerializer.Serialize(customDimensions) : string.Empty;
            _logger?.FailScenario(scenario.ScenarioName, scenario.ScenarioInstance, serialized);
        }

        /// <summary>
        /// Set the logger for PowerFx.
        /// This logger will recieve all logging calls from within PowerFx.
        /// </summary>
        public static void SetLogger(IPowerFxLogger logger)
        {
            Contracts.VerifyValue(logger);

            Instance._logger = logger;
        }

        /// <summary>
        /// Allows tests to reset the state of the logger
        /// Don't use in non-test code.
        /// </summary>
        internal static void TestOnly_Reset()
        {
            _instance = null;
        }
    }
}
