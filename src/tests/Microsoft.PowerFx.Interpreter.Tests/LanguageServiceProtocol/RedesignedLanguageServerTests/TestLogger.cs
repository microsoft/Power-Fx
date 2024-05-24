// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.LanguageServerProtocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestLogger : ILanguageServerLogger
    {
        private readonly List<string> _messages = new ();

        public List<string> Messages => _messages;

        public void LogError(string message, object data = null)
        {
            _messages.Add(message);
        }

        public void LogException(Exception exception, object data = null)
        {
            _messages.Add(exception.Message);
        }

        public void LogInformation(string message, object data = null)
        {
            _messages.Add(message);
        }

        public void LogWarning(string message, object data = null)
        {
            _messages.Add(message);
        }
    }
}
