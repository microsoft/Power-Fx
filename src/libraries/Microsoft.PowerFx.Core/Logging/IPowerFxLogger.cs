// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerFx.Core.Logging
{
    /// <summary>
    /// Interface used for hosts to collect Power Fx internal telemetry.
    /// Power Fx must ensure that anything logged via this interface
    /// is sanitized and does not contain user-data.
    /// </summary>
    public interface IPowerFxLogger
    {
        void Verbose(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Information(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Warning(Exception exception, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Warning(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Start(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Stop(string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Exception(Exception exception, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Exception(string message, Exception exception, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void Track(string name, string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void StartScenario(string name, string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void EndScenario(string name, string guid, string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void FailScenario(string name, string guid, string message, [CallerMemberName] string methodName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "");
        void LogMetric(string metricName, IDictionary<string, string> metricDimensions, long value);
    }
}
