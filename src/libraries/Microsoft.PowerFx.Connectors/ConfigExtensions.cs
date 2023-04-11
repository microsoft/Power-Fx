﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

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
        /// <param name="numberIsFloat">Number is float.</param>
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient = null, ICachingHttpClient cache = null, bool numberIsFloat = false)
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

            List<ServiceFunction> functions = OpenApiParser.Parse(functionNamespace, openApiDocument, httpClient, cache, numberIsFloat);
            foreach (ServiceFunction function in functions)
            {
                config.AddFunction(function);
            }

            List<FunctionInfo> functionInfos = functions.ConvertAll(function => new FunctionInfo(function));
            return functionInfos;
        }

        public static void AddService(this PowerFxConfig config, string functionNamespace, ConnectorFunction function, HttpMessageInvoker httpClient = null, ICachingHttpClient cache = null)
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
            
            config.AddFunction(function.GetServiceFunction(functionNamespace, httpClient, cache));
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
