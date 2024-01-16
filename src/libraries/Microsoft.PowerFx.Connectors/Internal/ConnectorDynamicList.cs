// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Internal class supporting "x-ms-dynamic-list" extension.
    /// https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values.
    /// </summary>
    internal class ConnectorDynamicList : ConnectorDynamicApi
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

        internal ConnectorDynamicList(OpenApiObject openApiObject)
            : base(openApiObject)
        {            
        }

        internal ConnectorDynamicList(string error)
            : base(error)
        {
        }
    }
}
