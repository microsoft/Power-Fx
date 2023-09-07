// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Microsoft.PowerFx.Interpreter.Functions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    internal class TestConnectorRuntimeContext : BaseRuntimeConnectorContext
    {
        private readonly Dictionary<string, HttpMessageInvoker> _clients = new ();
        private readonly bool _throwOnError;

        public TestConnectorRuntimeContext(string @namespace, HttpMessageInvoker client, bool? throwOnError = null)
        {
            Add(@namespace, client);
            _throwOnError = throwOnError ?? base.ThrowOnError;
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
    }
}
