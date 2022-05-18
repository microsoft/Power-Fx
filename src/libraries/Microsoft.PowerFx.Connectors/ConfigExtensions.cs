// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;

namespace Microsoft.PowerFx
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>. 
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication. 
        /// </summary>
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="functionNamespace">Namespace to place functions in.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="httpClient">Required iff we want to invoke the API. A client to invoke the endpoints described in the api document. This must handle auth or any other tranforms the API expects.</param>
        /// <param name="cache">A cache to avoid redundant HTTP gets.</param>
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient, ICachingHttpClient cache = null)
        {
            var functions = OpenApiParser.Parse(functionNamespace, openApiDocument, httpClient, cache);
            foreach (var function in functions)
            {
                config.AddFunction(function);
            }

            var functionInfos = functions.ConvertAll(function => new FunctionInfo(function));
            return functionInfos;
        }
    }
}
