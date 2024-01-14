// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-schema" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-schema.
    /// </summary>
    internal class ConnectorDynamicSchema : ConnectorDynamicApi
    {
        /// <summary>
        /// "value-path" in "x-ms-dynamic-schema".
        /// </summary>
        public string ValuePath = null;

        internal ConnectorDynamicSchema(OpenApiObject openApiObject)
            : base(openApiObject)
        {
        }

        internal ConnectorDynamicSchema(string error)
            : base(error)
        {
        }
    }
}
