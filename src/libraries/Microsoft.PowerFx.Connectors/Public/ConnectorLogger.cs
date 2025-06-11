// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Logger for connector operations.
    /// </summary>
    public abstract class ConnectorLogger
    {
        protected abstract void Log(ConnectorLog log);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public virtual void LogInformation(string message)
        {
            Log(new ConnectorLog(LogCategory.Information, message));
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public virtual void LogDebug(string message)
        {
            Log(new ConnectorLog(LogCategory.Debug, message));
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public virtual void LogError(string message)
        {
            Log(new ConnectorLog(LogCategory.Error, message));
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public virtual void LogWarning(string message)
        {
            Log(new ConnectorLog(LogCategory.Warning, message));
        }

        /// <summary>
        /// Logs an exception with a message.
        /// </summary>
        public virtual void LogException(Exception ex, string message)
        {
            Log(new ConnectorLog(LogCategory.Exception, message, ex));
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
