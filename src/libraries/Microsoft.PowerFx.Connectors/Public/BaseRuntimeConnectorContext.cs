// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Connectors.Execution;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class BaseRuntimeConnectorContext
    {   
        public abstract object GetInvoker(string @namespace);

        public abstract TimeZoneInfo TimeZoneInfo { get; }

        public virtual bool ThrowOnError { get; } = false;

        internal virtual bool ReturnRawResults { get; } = false;

        public virtual ConnectorLogger ExecutionLogger { get; } = null;

        internal virtual IConnectorInvoker GetInvoker(ConnectorFunction function) => new HttpFunctionInvoker(function, this);

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
