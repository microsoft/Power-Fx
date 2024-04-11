﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ConnectorHelperFunctions;

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
        ///
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="connectorSettings">Connector settings containing Namespace and MaxRows to be returned.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="configurationLogger">Logger.</param>
        /// <returns>List of connector functions.</returns>
        public static IReadOnlyList<ConnectorFunction> AddActionConnector(this PowerFxConfig config, ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null)
        {
            return config.AddActionConnector(connectorSettings, openApiDocument, null, configurationLogger);
        }

        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>.
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication.
        /// </summary>
        ///
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="connectorSettings">Connector settings containing Namespace and MaxRows to be returned.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="globalValues">Global Values.</param>
        /// <param name="configurationLogger">Logger.</param>
        /// <returns>List of connector functions.</returns>
        public static IReadOnlyList<ConnectorFunction> AddActionConnector(this PowerFxConfig config, ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, ConnectorLogger configurationLogger = null)
        {
            try
            {
                configurationLogger?.LogInformation($"Entering in ConfigExtensions.{nameof(AddActionConnector)}, with {nameof(ConnectorSettings)} {LogConnectorSettings(connectorSettings)}");
                IReadOnlyList<ConnectorFunction> connectorFunctions = AddActionConnectorInternal(config, connectorSettings, openApiDocument, configurationLogger, globalValues);
                configurationLogger?.LogInformation($"Exiting ConfigExtensions.{nameof(AddActionConnector)}, returning {connectorFunctions.Count()} functions");

                return connectorFunctions;
            }
            catch (Exception ex)
            {
                configurationLogger?.LogException(ex, $"Exception in ConfigExtensions.{nameof(AddActionConnector)}, {nameof(ConnectorSettings)} {LogConnectorSettings(connectorSettings)}, {LogException(ex)}");
                throw;
            }
        }

        internal static IReadOnlyList<ConnectorFunction> AddActionConnectorInternal(this PowerFxConfig config, ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null, IReadOnlyDictionary<string, FormulaValue> globalValues = null)
        {
            if (config == null)
            {
                configurationLogger?.LogError($"PowerFxConfig is null, cannot add functions");
                return null;
            }

            (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) = OpenApiParser.ParseInternal(connectorSettings, openApiDocument, configurationLogger, globalValues);
            foreach (TexlFunction function in texlFunctions)
            {
                config.AddFunction(function);
            }

            return connectorFunctions;
        }

        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>.
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication.
        /// </summary>
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="namespace">Namespace name.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="configurationLogger">Logger.</param>
        /// <returns>List of connector functions.</returns>
        public static IReadOnlyList<ConnectorFunction> AddActionConnector(this PowerFxConfig config, string @namespace, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null)
        {
            return config.AddActionConnector(@namespace, openApiDocument, null, configurationLogger);
        }

        /// <summary>
        /// Add functions for each operation in the <see cref="OpenApiDocument"/>.
        /// Functions names will be 'functionNamespace.operationName'.
        /// Functions are invoked via rest via the httpClient. The client must handle authentication.
        /// </summary>
        /// <param name="config">Config to add the functions to.</param>
        /// <param name="namespace">Namespace name.</param>
        /// <param name="openApiDocument">An API document. This can represent multiple formats, including Swagger 2.0 and OpenAPI 3.0.</param>
        /// <param name="globalValues">Global Values.</param>
        /// <param name="configurationLogger">Logger.</param>
        /// <returns>List of connector functions.</returns>
        public static IReadOnlyList<ConnectorFunction> AddActionConnector(this PowerFxConfig config, string @namespace, OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, ConnectorLogger configurationLogger = null)
        {
            try
            {
                configurationLogger?.LogInformation($"Entering in ConfigExtensions.{nameof(AddActionConnector)}, with {nameof(ConnectorSettings)} Namespace {@namespace ?? Null(nameof(@namespace))}");
                IReadOnlyList<ConnectorFunction> connectorFunctions = AddActionConnectorInternal(config, new ConnectorSettings(@namespace), openApiDocument, configurationLogger, globalValues);

                if (connectorFunctions == null)
                {
                    return null;
                }

                configurationLogger?.LogInformation($"Exiting ConfigExtensions.{nameof(AddActionConnector)}, returning {connectorFunctions.Count()} functions");
                return connectorFunctions;
            }
            catch (Exception ex)
            {
                configurationLogger?.LogException(ex, $"Exception in ConfigExtensions.{nameof(AddActionConnector)}, Namespace {@namespace ?? Null(nameof(@namespace))}, {LogException(ex)}");
                throw;
            }
        }
    }
}
