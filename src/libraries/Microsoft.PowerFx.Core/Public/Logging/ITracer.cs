// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Logging
{
    /// <summary>
    /// Interface for tracing and logging events.
    /// </summary>
    public interface ITracer
    {
        /// <summary>
        /// Asynchronously logs a message with the specified severity and custom record.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="serv">The severity of the trace event.</param>
        /// <param name="customRecord">A custom record value to include with the log entry.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous log operation.</returns>
        public Task LogAsync(string message, TraceSeverity serv, RecordValue customRecord, CancellationToken ct);
    }

    /// <summary>
    /// Specifies the severity level of a trace event.
    /// </summary>
    public enum TraceSeverity
    {
        /// <summary>
        /// Critical severity, indicating a critical error or failure.
        /// </summary>
        Critical = -1,

        /// <summary>
        /// Error severity, indicating an error event.
        /// </summary>
        Error = 0,

        /// <summary>
        /// Warning severity, indicating a warning event.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Informational severity, indicating an informational event.
        /// </summary>
        Information = 3
    }
}
