// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-properties" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-schema.
    /// </summary>
    internal class ConnectorDynamicProperty : ConnectionDynamicApi
    {
        /// <summary>
        /// "itemValuePath" in "x-ms-dynamic-properties".
        /// </summary>
        public string ItemValuePath = null;
    }
}
