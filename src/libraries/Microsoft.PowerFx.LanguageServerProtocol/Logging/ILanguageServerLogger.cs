// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Logger interface for language server.
    /// </summary>
    public interface ILanguageServerLogger
    {
        /// <summary>
        /// Log information message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="data">Additional Data to log.</param>
        public void LogInformation(string message, object data = null);

        /// <summary>
        /// Log warning message.
        /// </summary>
        /// <param name="message">Warning Message.</param>
       /// <param name="data">Additional Data to log.</param>
        public void LogWarning(string message, object data = null);

        /// <summary>
        /// Log error message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="data">Additional Data to log.</param>
        public void LogError(string message, object data = null);

        /// <summary>
        /// Log exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="data">Additional Data to log.</param>
        public void LogException(Exception exception, object data = null);
    }
}
