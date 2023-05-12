// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.AppMagic.Authoring.Texl.Builtins
{
    internal class ConnectorDynamicValue : ConnectionDynamicApi
    {
        /// <summary>
        /// "value-title" in "x-ms-dynamic-values".
        /// </summary>
        public string ValueTitle = null;

        /// <summary>
        /// "value-path" in "x-ms-dynamic-values".
        /// </summary>
        public string ValuePath = null;

        /// <summary>
        /// "value-collection" in "x-ms-dynamic-values".
        /// </summary>
        public string ValueCollection = null;        
    }
}
