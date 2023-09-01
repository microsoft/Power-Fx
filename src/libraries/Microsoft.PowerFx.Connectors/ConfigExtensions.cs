// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    public static class ConfigExtensions
    {
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument)
        {
            return config.AddService(functionNamespace, openApiDocument, null, new ConnectorSettings());
        }
        
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient)
        {
            return config.AddService(functionNamespace, openApiDocument, httpClient, new ConnectorSettings());
        }        

        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient, ICachingHttpClient cache, bool numberIsFloat)
        {
            return config.AddService(functionNamespace, openApiDocument, httpClient, new ConnectorSettings() { Cache = cache, NumberIsFloat = numberIsFloat });
        }

        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>. 
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication. 
        /// </summary>
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="functionNamespace">Namespace to place functions in.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="httpClient">Required iff we want to invoke the API. A client to invoke the endpoints described in the api document. This must handle auth or any other tranforms the API expects.</param>
        /// <param name="connectorSettings">Connector settings containing cache, numberIsFloat and MaxRows to be returned.</param>        
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient, ConnectorSettings connectorSettings)
        {
            if (functionNamespace == null)
            {
                throw new ArgumentNullException(nameof(functionNamespace));
            }

            if (!DName.IsValidDName(functionNamespace))
            {
                throw new ArgumentException(nameof(functionNamespace), $"invalid functionNamespace: {functionNamespace}");
            }

            if (openApiDocument == null)
            {
                throw new ArgumentNullException(nameof(openApiDocument));
            }
            
            List<ServiceFunction> functions = OpenApiParser.Parse(functionNamespace, openApiDocument, httpClient, connectorSettings.Clone(@namespace: functionNamespace));
            foreach (ServiceFunction function in functions)
            {
                config.AddFunction(function);
            }

            List<FunctionInfo> functionInfos = functions.ConvertAll(function => new FunctionInfo(function));
            return functionInfos;
        }

        public static void AddService(this PowerFxConfig config, string functionNamespace, ConnectorFunction function)
        {
            config.AddService(functionNamespace, function, null, new ConnectorSettings());
        }

        public static void AddService(this PowerFxConfig config, string functionNamespace, ConnectorFunction function, HttpMessageInvoker httpClient)
        {
            config.AddService(functionNamespace, function, httpClient, new ConnectorSettings());
        }

        public static void AddService(this PowerFxConfig config, string functionNamespace, ConnectorFunction function, HttpMessageInvoker httpClient, ICachingHttpClient cache)
        {
            config.AddService(functionNamespace, function, httpClient, new ConnectorSettings() { Cache = cache });
        }

        public static void AddService(this PowerFxConfig config, string functionNamespace, ConnectorFunction function, HttpMessageInvoker httpClient, ConnectorSettings connectorSettings)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!DName.IsValidDName(functionNamespace))
            {
                throw new ArgumentException(nameof(functionNamespace), $"invalid functionNamespace: {functionNamespace}");
            }

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            config.AddFunction(function.GetServiceFunction(httpClient, connectorSettings));
        }

        public static void Add(this Dictionary<string, FormulaValue> map, string fieldName, FormulaValue value)
        {
            if (map.ContainsKey(fieldName))
            {
                throw new InvalidOperationException($"Invalid schema, two parameters have the same name {fieldName}");
            }

            map[fieldName] = value;
        }
    }
}
