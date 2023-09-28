// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.PowerFx.Connectors
{
    internal class RuntimeConnectorContextWithRawResults : BaseRuntimeConnectorContext
    {
        private readonly BaseRuntimeConnectorContext _baseRuntimeConnectorContext;

        internal RuntimeConnectorContextWithRawResults(BaseRuntimeConnectorContext baseRuntimeConnectorContext)
        {
            _baseRuntimeConnectorContext = baseRuntimeConnectorContext;
        }

        public override TimeZoneInfo TimeZoneInfo => _baseRuntimeConnectorContext.TimeZoneInfo;

        public override HttpMessageInvoker GetInvoker(string @namespace) => _baseRuntimeConnectorContext.GetInvoker(@namespace);

        public override bool ThrowOnError => _baseRuntimeConnectorContext.ThrowOnError;

        internal override bool ReturnRawResults => true;

        public override ConnectorLogger ExecutionLogger => _baseRuntimeConnectorContext.ExecutionLogger;
    }
}
