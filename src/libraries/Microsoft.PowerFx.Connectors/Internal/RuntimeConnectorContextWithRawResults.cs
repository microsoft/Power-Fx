﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Connectors.Execution;

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

        public override FunctionInvoker GetInvoker(ConnectorFunction function, bool returnRawResults) => _baseRuntimeConnectorContext.GetInvoker(function, true);

        public override bool ThrowOnError => _baseRuntimeConnectorContext.ThrowOnError;

        internal override bool ReturnRawResults => true;

        public override ConnectorLogger ExecutionLogger => _baseRuntimeConnectorContext.ExecutionLogger;
    }
}
