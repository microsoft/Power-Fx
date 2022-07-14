// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerFx.Core.Logging
{
    /// <summary>
    /// A logger which logs to the Debug window in debug mode.
    /// </summary>
    public class DebugLogger : IPowerFxLogger
    {
        public void Verbose(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Log("Verbose", message);
        }

        public void Information(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Log("Info", message);
        }

        public void Warning(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Log("Warning", message);
        }

        public void Warning(Exception exception, string methodName, int sourceLineNumber, string sourceFilePath)
        {
            Log("Warning", exception.ToString());
        }

        public void Start(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Log("Start", message);
        }

        public void Stop(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Log("Stop", message);
        }

        public void Exception(Exception exception, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Contracts.AssertValue(exception);
            Log("Exception", exception.ToString());
        }

        public void Exception(string message, Exception exception, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            Contracts.AssertValue(exception);
            Contracts.AssertValue(message);
            Log("Exception:" + message, exception.ToString());
        }

        public void Track(string name, string message, string methodName, int sourceLineNumber, string sourceFilePath)
        {
            Log("Track", $"{name}: {message}");
        }

        public void StartScenario(string name, string message, string methodName, int sourceLineNumber, string sourceFilePath)
        {
            Log("StartScenario", $"{name}: {message}");
        }

        public void EndScenario(string name, string guid, string message, string methodName, int sourceLineNumber, string sourceFilePath)
        {
            Log("EndScenario", $"{name}/{guid}: {message}");
        }

        public void FailScenario(string name, string guid, string message, string methodName, int sourceLineNumber, string sourceFilePath)
        {
            Log("FailScenario", $"{name}/{guid}): {message}");
        }

        public void LogMetric(string metricName, IDictionary<string, string> metricDimensions, long value)
        {
            Log($"LogMetric:{metricName}/{metricDimensions}", $"{value}");
        }

        private void Log(string type, string message)
        {
            Contracts.AssertValue(type);
            Contracts.AssertValue(message);

#if DEBUG
            Debug.WriteLine("[{0}] {1}", type, message);
#endif
        }
    }
}
