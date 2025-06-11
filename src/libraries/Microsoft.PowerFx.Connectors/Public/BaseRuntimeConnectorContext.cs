// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Base class for runtime connector context.
    /// </summary>
    public abstract class BaseRuntimeConnectorContext
    {
        /// <summary>
        /// Gets the HTTP message invoker for the specified namespace.
        /// </summary>
        public abstract HttpMessageInvoker GetInvoker(string @namespace);

        /// <summary>
        /// Gets the time zone information.
        /// </summary>
        public abstract TimeZoneInfo TimeZoneInfo { get; }

        /// <summary>
        /// Gets a value indicating whether to throw on error.
        /// </summary>
        public virtual bool ThrowOnError { get; } = false;        

        internal virtual bool ReturnRawResults { get; } = false;        

        public virtual ConnectorLogger ExecutionLogger { get; } = null; 

        internal BaseRuntimeConnectorContext WithRawResults()
        {
            return new RuntimeConnectorContextWithRawResults(this);
        }
    }

    /// <summary>
    /// Extension methods for runtime connector context.
    /// </summary>
    public static class RuntimeConnectorContextExtensions
    {
        /// <summary>
        /// Adds a runtime context to the service provider.
        /// </summary>
        public static BasicServiceProvider AddRuntimeContext(this BasicServiceProvider serviceProvider, BaseRuntimeConnectorContext context)
        {
            return serviceProvider.AddService(typeof(BaseRuntimeConnectorContext), context);
        }
    }
}
