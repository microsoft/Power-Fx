// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<ConnectorLog> _logs = new ();

        internal ConsoleLogger(ITestOutputHelper console, bool includeDebug = false)
        {
            _console = console;
            _includeDebug = includeDebug;
        }       

        internal string GetLogs()
        {
            return string.Join("|", _logs.Select(cl => GetMessage(cl)).Where(m => m != null));
        }

        private string GetMessage(ConnectorLog connectorLog)
        {           
            string cat = connectorLog.Category switch
            {
                LogCategory.Exception => "EXCPT",
                LogCategory.Error => "ERROR",
                LogCategory.Warning => "WARN ",
                LogCategory.Information => "INFO ",
                LogCategory.Debug => "DEBUG",
                _ => "??"
            };

            return $"[{cat}] {connectorLog.Message}";        
        }

        public override void Log(ConnectorLog log)
        {            
            if (_includeDebug || log.Category != LogCategory.Debug)
            {
                _console.WriteLine(GetMessage(log));
                _logs.Add(log);
            }
        }
    }
}
