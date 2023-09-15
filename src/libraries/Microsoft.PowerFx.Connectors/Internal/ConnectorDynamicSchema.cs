// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-schema" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-schema.
    /// </summary>
    internal class ConnectorDynamicSchema : ConnectionDynamicApi
    {
        /// <summary>
        /// "value-path" in "x-ms-dynamic-schema".
        /// </summary>
        public string ValuePath = null;
    }
}
