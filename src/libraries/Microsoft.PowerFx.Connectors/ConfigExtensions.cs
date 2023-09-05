﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    public static class ConfigExtensions
    {
        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>. 
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication. 
        /// </summary>
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="connectorSettings">Connector settings containing cache, numberIsFloat and MaxRows to be returned.</param>        
        public static IReadOnlyList<FunctionInfo> AddService(this PowerFxConfig config, ConnectorSettings connectorSettings, OpenApiDocument openApiDocument)
        {
            if (connectorSettings.Namespace == null)
            {
                throw new ArgumentNullException(nameof(connectorSettings.Namespace));
            }

            if (!DName.IsValidDName(connectorSettings.Namespace))
            {
                throw new ArgumentException(nameof(connectorSettings.Namespace), $"invalid functionNamespace: {connectorSettings.Namespace}");
            }

            if (openApiDocument == null)
            {
                throw new ArgumentNullException(nameof(openApiDocument));
            }

            List<TexlFunction> functions = OpenApiParser.Parse(connectorSettings, openApiDocument);
            foreach (TexlFunction function in functions)
            {
                config.AddFunction(function);
            }

            List<FunctionInfo> functionInfos = functions.ConvertAll(function => new FunctionInfo(function));
            return functionInfos;
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
