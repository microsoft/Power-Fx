// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
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

        /// <summary>
        /// "capability" in "x-ms-dynamic-values".
        /// https://github.com/nk-gears/pa-custom-connector-filepicker/blob/main/README.md.
        /// https://reenhanced.com/2021/power-automate-secrets-how-to-implement-a-custom-file-picker-undocumented/.
        /// </summary>
        public string Capability = null;

        /// <summary>
        /// "builtInOperation" in "x-ms-dynamic-values".
        /// </summary>
        public string BuiltInOperation = null;
    }
}
