// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorDynamicSchema : ConnectionDynamicApi
    {
        /// <summary>
        /// "value-path" in "x-ms-dynamic-schema".
        /// </summary>
        public string ValuePath = null;
    }
}
