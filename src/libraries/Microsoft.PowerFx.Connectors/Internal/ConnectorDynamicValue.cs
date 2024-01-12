// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-values" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values.
    /// </summary>
    internal class ConnectorDynamicValue : ConnectorDynamicApi
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

        /* Support the following two parameters some day in the future
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
        */

        internal ConnectorDynamicValue(OpenApiObject openApiObject)
            : base(openApiObject)
        {
        }

        internal ConnectorDynamicValue(string error)
            : base(error)
        {
        }
    }
}
