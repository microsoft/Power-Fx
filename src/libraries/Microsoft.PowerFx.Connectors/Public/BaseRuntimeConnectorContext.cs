// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using Microsoft.PowerFx.Connectors.Execution;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class BaseRuntimeConnectorContext
    {
        // When implementing a custom invoker, this is the method to override
        public virtual FunctionInvoker GetInvoker(ConnectorFunction function, bool returnRawResults = false)
        {
            HttpMessageInvoker httpInvoker = GetHttpInvoker(function);
            return new HttpFunctionInvoker(function, returnRawResults ? WithRawResults() : this, httpInvoker ?? throw new PowerFxConnectorException("If not overriding GetInvoker, you must implement GetCustomInvoker"));
        }

        // When using default http invoker, this is the method to override
        public virtual HttpMessageInvoker GetHttpInvoker(ConnectorFunction function) => null;

        public abstract TimeZoneInfo TimeZoneInfo { get; }

        public virtual bool ThrowOnError { get; } = false;

        internal virtual bool ReturnRawResults { get; } = false;

        public virtual ConnectorLogger ExecutionLogger { get; } = null;

        internal BaseRuntimeConnectorContext WithRawResults()
        {
            return new RuntimeConnectorContextWithRawResults(this);
        }
    }

    public static class RuntimeConnectorContextExtensions
    {
        public static BasicServiceProvider AddRuntimeContext(this BasicServiceProvider serviceProvider, BaseRuntimeConnectorContext context)
        {
            return serviceProvider.AddService(typeof(BaseRuntimeConnectorContext), context);
        }
    }
}
