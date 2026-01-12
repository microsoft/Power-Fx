// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class BaseRuntimeConnectorContext
    {
        public abstract HttpMessageInvoker GetInvoker(string @namespace);

        public abstract TimeZoneInfo TimeZoneInfo { get; }

        public virtual bool ThrowOnError { get; } = false;

        /// <summary>
        /// Gets a value indicating whether primitive values are allowed for properties defined as object types in the connector schema.
        /// When <c>true</c>, allows primitive values (string, number, boolean, etc.) to be serialized for properties defined as object type,
        /// which is useful for connectors that don't support oneOf in OpenAPI v2.0 and define properties as object type even though the value is primitive.
        /// </summary>
        public virtual bool AllowPrimitiveValuesForObjectTypes { get; } = false;

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
