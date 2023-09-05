// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorDynamicList : ConnectionDynamicApi
    {
        /// <summary>
        /// "itemTitlePath" in "x-ms-dynamic-list".
        /// </summary>
        public string ItemTitlePath = null;

        /// <summary>
        /// "itemsPath" in "x-ms-dynamic-list".
        /// </summary>
        public string ItemPath = null;

        /// <summary>
        /// "itemValuePath" in "x-ms-dynamic-list".
        /// </summary>
        public string ItemValuePath = null;
    }
}
