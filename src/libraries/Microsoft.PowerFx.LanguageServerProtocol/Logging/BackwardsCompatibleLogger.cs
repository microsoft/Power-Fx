// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Backwards compatible logger that can be used in place of the ILanguageServerLogger.
    /// </summary>
    internal sealed class BackwardsCompatibleLogger : ILanguageServerLogger
    {
        private readonly Action<string> _logger;

        public BackwardsCompatibleLogger(Action<string> logger = null)
        {
            _logger = logger;
        }

        private void Log(string message)
        {
            _logger?.Invoke(message);
        }

        public void LogError(string message, object data = null)
        {
            Log(message);
        }

        public void LogException(Exception exception, object data = null)
        {
            Log(exception.Message);
        }

        public void LogInformation(string message, object data = null)
        {
            Log(message);
        }

        public void LogWarning(string message, object data = null)
        {
            Log(message);
        }
    }
}
