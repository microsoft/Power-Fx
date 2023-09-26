// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    internal class TestConnectorRuntimeContext : BaseRuntimeConnectorContext
    {
        private readonly Dictionary<string, HttpMessageInvoker> _clients = new ();
        private readonly bool _throwOnError;
        private readonly ConnectorLogger _logger;

        public TestConnectorRuntimeContext(string @namespace, HttpMessageInvoker client, bool? throwOnError = null, ITestOutputHelper console = null, bool includeDebug = false)
        {
            Add(@namespace, client);
            _throwOnError = throwOnError ?? base.ThrowOnError;
            _logger = console == null ? null : new ConsoleLogger(console, includeDebug);
        }

        public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        public TestConnectorRuntimeContext Add(string @namespace, HttpMessageInvoker client)
        {
            _clients[string.IsNullOrEmpty(@namespace) ? throw new ArgumentException("Invalid namespace", nameof(@namespace)) : @namespace] = client ?? throw new ArgumentNullException(nameof(@namespace), "Invalid HttpMessageInvoker");
            return this;
        }

        public override HttpMessageInvoker GetInvoker(string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace) || !_clients.ContainsKey(@namespace))
            {
                throw new ArgumentException("Invalid namespace or missing HttpMessageInvoker for this namespace", nameof(@namespace));
            }

            return _clients[@namespace];
        }

        public override bool ThrowOnError => _throwOnError;

        public override ConnectorLogger ExecutionLogger => _logger;
    }

    internal class ConsoleLogger : ConnectorLogger
    {
        private readonly ITestOutputHelper _console;
        private readonly bool _includeDebug;
        internal List<string> Logs = new List<string>();

        internal ConsoleLogger(ITestOutputHelper console, bool includeDebug = false)
        {
            _console = console;
            _includeDebug = includeDebug;
        }

        private void Log(string message)
        {
            _console.WriteLine(message);
            Logs.Add(message);
        }

        internal string GetLogs()
        {
            return string.Join("|", Logs);
        }

        public override void LogDebug(Guid id, string message)
        {
            if (_includeDebug)
            {
                Log($"[DEBUG] {id} {message}");
            }
        }

        public override void LogError(Guid id, string message, Exception ex = null)
        {
            Log(ex == null ? $"[ERROR] {id} {message}" : $"[ERROR] {id} {message} - Exception {ex.GetType().FullName}, Message {ex.Message}, Callstack {ex.StackTrace}");
        }

        public override void LogInformation(Guid id, string message)
        {
            Log($"[INFO ] {id} {message}");
        }

        public override void LogWarning(Guid id, string message)
        {
            Log($"[WARN ] {id} {message}");
        }
    }
}
