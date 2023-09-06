// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace Microsoft.PowerFx.Connectors.Tests
{
    internal class TestConnectorRuntimeContext : IRuntimeConnectorContext
    {
        private readonly Dictionary<string, HttpMessageInvoker> _clients = new ();

        public TestConnectorRuntimeContext(string @namespace, HttpMessageInvoker client)
        {
            Add(@namespace, client);
        }

        public TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        public CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        public TestConnectorRuntimeContext Add(string @namespace, HttpMessageInvoker client)
        {
            _clients[string.IsNullOrEmpty(@namespace) ? throw new ArgumentException("Invalid namespace", nameof(@namespace)) : @namespace] = client ?? throw new ArgumentNullException(nameof(@namespace), "Invalid HttpMessageInvoker");
            return this;
        }

        public HttpMessageInvoker GetInvoker(string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace) || !_clients.ContainsKey(@namespace))
            {
                throw new ArgumentException("Invalid namespace or missing HttpMessageInvoker for this namespace", nameof(@namespace));
            }

            return _clients[@namespace];
        }
    }
}
