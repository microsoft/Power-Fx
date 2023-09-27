// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class ConnectorLogger
    {
        public abstract void Log(ConnectorLog log);

        public virtual void LogInformation(string message)
        {
            Log(new ConnectorLog(LogCategory.Information, message));
        }

        public virtual void LogDebug(string message)
        {
            Log(new ConnectorLog(LogCategory.Debug, message));
        }

        public virtual void LogError(string message)
        {
            Log(new ConnectorLog(LogCategory.Error, message));
        }

        public virtual void LogWarning(string message)
        {
            Log(new ConnectorLog(LogCategory.Warning, message));
        }

        public virtual void LogException(Exception ex, string message)
        {
            Log(new ConnectorLog(LogCategory.Warning, message, ex));
        }
    }

    public class ConnectorLog
    {
        internal ConnectorLog(LogCategory category, string message, Exception exception = null)
        {
            Category = category;
            Message = message;
            Exception = exception;
        }

        public LogCategory Category { get; internal set; }

        public string Message { get; internal set; }

        public Exception Exception { get; internal set; }
    }

    public enum LogCategory
    {
        Exception,
        Error,
        Warning,
        Information,
        Debug
    }
}
