// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-properties" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-schema.
    /// </summary>
    internal class ConnectorDynamicProperty : ConnectorDynamicApi
    {
        /// <summary>
        /// "itemValuePath" in "x-ms-dynamic-properties".
        /// </summary>
        public string ItemValuePath = null;

        internal ConnectorDynamicProperty(OpenApiObject openApiObject)
            : base(openApiObject)
        {
        }

        internal ConnectorDynamicProperty(string error)
            : base(error)
        {
        }
    }
}
