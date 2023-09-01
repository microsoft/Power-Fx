// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Authoring.Texl.Builtins;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorDynamicProperty : ConnectionDynamicApi
    {
        /// <summary>
        /// "itemValuePath" in "x-ms-dynamic-properties".
        /// </summary>
        public string ItemValuePath = null;
    }
}
