// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.PowerFx.Connectors.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    internal class TestConnectorRuntimeContext : BaseRuntimeConnectorContext
    {
        private readonly Dictionary<string, HttpMessageInvoker> _clients = new ();
        private bool _throwOnError;

        public TestConnectorRuntimeContext(RuntimeConfig runtimeConfig, ITestOutputHelper console = null, bool includeDebug = false)
            : base(Initialize(runtimeConfig, console, includeDebug))
        {
        }

        private static RuntimeConfig Initialize(RuntimeConfig runtimeConfig, ITestOutputHelper console, bool includeDebug)
        {
            Assert.False(runtimeConfig == null || runtimeConfig.ServiceProvider == null);

            if (runtimeConfig.ServiceProvider.GetService<TimeZoneInfo>() == null)
            {
                runtimeConfig.ServiceProvider.AddService(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            }

            if (runtimeConfig.ServiceProvider.GetService<ConnectorLogger>() == null && console != null)
            {
                runtimeConfig.ServiceProvider.AddService<ConnectorLogger>(new ConsoleLogger(console, includeDebug));
            }

            return runtimeConfig;
        }

        public TestConnectorRuntimeContext Add(string @namespace, HttpMessageInvoker client, bool? throwOnError = null)
        {            
            _throwOnError = throwOnError ?? base.ThrowOnError;
            _clients[string.IsNullOrEmpty(@namespace) ? throw new ArgumentException("Invalid namespace", nameof(@namespace)) : @namespace] = client ?? throw new ArgumentNullException(nameof(@namespace), "Invalid HttpMessageInvoker");
            return this;
        }

        public override FunctionInvoker GetInvoker(ConnectorFunction function, bool rawResults)
        {
            if (string.IsNullOrEmpty(function.InvokerSignature) || !_clients.ContainsKey(function.InvokerSignature))
            {
                throw new ArgumentException("Invalid namespace or missing HttpMessageInvoker for this namespace", nameof(function.Namespace));
            }

            return GetDefaultInvoker(function, this, _clients[function.Namespace], rawResults);
        }

        public override bool ThrowOnError => _throwOnError;
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

        protected override void Log(ConnectorLog log)
        {
            if (_includeDebug || log.Category != LogCategory.Debug)
            {
                _console.WriteLine(GetMessage(log));
                _logs.Add(log);
            }
        }
    }
}
