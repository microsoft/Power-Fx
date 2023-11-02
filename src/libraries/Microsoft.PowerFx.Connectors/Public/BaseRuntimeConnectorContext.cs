// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using Microsoft.PowerFx.Connectors.Execution;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Base class used to create the runtime context for connectors.
    /// </summary>    
    public abstract class BaseRuntimeConnectorContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRuntimeConnectorContext"/> class.
        /// </summary>
        /// <param name="runtimeConfig">Runtime configuration.</param>
        /// <remarks>We use the service provider of the runtime configuration
        /// To retrieve TimeZoneInfo or ConnectorLogger services.</remarks>
        public BaseRuntimeConnectorContext(RuntimeConfig runtimeConfig)
        {
            if (runtimeConfig == null || runtimeConfig.ServiceProvider == null)
            {
                TimeZoneInfo = TimeZoneInfo.Utc;
                ExecutionLogger = null;
            }

            TimeZoneInfo = runtimeConfig.ServiceProvider.GetService<TimeZoneInfo>();
            ExecutionLogger = runtimeConfig.ServiceProvider.GetService<ConnectorLogger>();
        }

        /// <summary>
        /// Returns the function invoker that is submitting the connector function call.        
        /// </summary>
        /// <param name="function">Connector function.</param>
        /// <param name="returnsRawResult">Should return raw result.</param>
        /// <returns>Function invoker.</returns>
        public abstract FunctionInvoker GetInvoker(ConnectorFunction function, bool returnsRawResult = false);

        /// <summary>
        /// Lets the end user decide if they want to throw on error or not.
        /// </summary>
        public virtual bool ThrowOnError { get; } = false;

        internal TimeZoneInfo TimeZoneInfo { get; }

        internal ConnectorLogger ExecutionLogger { get; }

        /// <summary>
        /// Generates a default HTTP function invoker.
        /// </summary>
        /// <param name="function">Connector function.</param>
        /// <param name="context">BaseRuntimeConnectorContext to use.</param>
        /// <param name="httpInvoker">HTTP message invoker.</param>
        /// <param name="rawResults">Flag to toggle raw output.</param>
        /// <returns></returns>
        public static FunctionInvoker GetDefaultInvoker(ConnectorFunction function, BaseRuntimeConnectorContext context, HttpMessageInvoker httpInvoker, bool rawResults) 
            => new HttpFunctionInvoker(function, context, rawResults, httpInvoker);
    }

    public static class RuntimeConnectorContextExtensions
    {
        public static BasicServiceProvider AddRuntimeContext(this BasicServiceProvider serviceProvider, BaseRuntimeConnectorContext context)
        {
            return serviceProvider.AddService(typeof(BaseRuntimeConnectorContext), context);
        }
    }
}
