// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ConnectorHelperFunctions;
using static Microsoft.PowerFx.Connectors.Constants;
using static Microsoft.PowerFx.Connectors.OpenApiHelperFunctions;

namespace Microsoft.PowerFx.Connectors
{
    public class OpenApiParser
    {
        public static IEnumerable<ConnectorFunction> GetFunctions(string @namespace, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null)
        {
            return GetFunctions(@namespace, openApiDocument, null, configurationLogger);
        }

        public static IEnumerable<ConnectorFunction> GetFunctions(string @namespace, OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, ConnectorLogger configurationLogger = null)
        {
            try
            {
                configurationLogger?.LogInformation($"Entering in {nameof(OpenApiParser)}.{nameof(GetFunctions)}, with {nameof(ConnectorSettings)} Namespace {@namespace}");
                IEnumerable<ConnectorFunction> functions = GetFunctionsInternal(new ConnectorSettings(@namespace), openApiDocument, configurationLogger, globalValues);
                configurationLogger?.LogInformation($"Exiting {nameof(OpenApiParser)}.{nameof(GetFunctions)}, with {nameof(ConnectorSettings)} Namespace {@namespace}, returning {functions.Count()} functions");
                return functions.Where(f => ShouldIncludeFunction(f));
            }
            catch (Exception ex)
            {
                configurationLogger?.LogException(ex, $"Exception in {nameof(OpenApiParser)}.{nameof(GetFunctions)}, {nameof(ConnectorSettings)} Namespace {@namespace}, {LogException(ex)}");
                throw;
            }
        }

        public static IEnumerable<ConnectorFunction> GetFunctions(ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null)
        {
            return GetFunctions(connectorSettings, openApiDocument, null, configurationLogger);
        }

        public static IEnumerable<ConnectorFunction> GetFunctions(ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, ConnectorLogger configurationLogger = null)
        {
            try
            {
                configurationLogger?.LogInformation($"Entering in {nameof(OpenApiParser)}.{nameof(GetFunctions)}, with {nameof(ConnectorSettings)} {LogConnectorSettings(connectorSettings)}");
                IEnumerable<ConnectorFunction> functions = GetFunctionsInternal(connectorSettings, openApiDocument, configurationLogger, globalValues);
                configurationLogger?.LogInformation($"Exiting {nameof(OpenApiParser)}.{nameof(GetFunctions)}, with {nameof(ConnectorSettings)} {LogConnectorSettings(connectorSettings)}, returning {functions.Count()} functions");
                return functions.Where(f => ShouldIncludeFunction(f, connectorSettings));
            }
            catch (Exception ex)
            {
                configurationLogger?.LogException(ex, $"Exception in {nameof(OpenApiParser)}.{nameof(GetFunctions)}, {nameof(ConnectorSettings)} {LogConnectorSettings(connectorSettings)}, {LogException(ex)}");
                throw;
            }
        }

        private static bool ShouldIncludeFunction(ConnectorFunction function, ConnectorSettings settings = null)
        {
            // By default, internal & unsupported functions are excluded
            // We don't use IsDeprecated here as those functions should be returned by default
            return (!function.IsInternal || settings?.IncludeInternalFunctions == true) &&
                   (function.IsSupported || settings?.AllowUnsupportedFunctions == true);
        }

        internal static IEnumerable<ConnectorFunction> GetFunctionsInternal(ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null, IReadOnlyDictionary<string, FormulaValue> globalValues = null)
        {
            bool connectorIsSupported = true;
            string connectorNotSupportedReason = string.Empty;
            List<ConnectorFunction> functions = new ();

            if (connectorSettings == null)
            {
                configurationLogger?.LogError($"{nameof(connectorSettings)} is null");
                return functions;
            }

            if (connectorSettings.Namespace == null)
            {
                configurationLogger?.LogError($"{nameof(connectorSettings)}.{nameof(connectorSettings.Namespace)} is null");
                return functions;
            }

            if (!DName.IsValidDName(connectorSettings.Namespace))
            {
                configurationLogger?.LogError($"{nameof(connectorSettings)}.{nameof(connectorSettings.Namespace)} is not a valid DName");
                return functions;
            }

            if (openApiDocument == null)
            {
                configurationLogger?.LogError($"{nameof(openApiDocument)} is null");
                return functions;
            }

            if (!ValidateSupportedOpenApiDocument(openApiDocument, ref connectorIsSupported, ref connectorNotSupportedReason, connectorSettings.FailOnUnknownExtension, configurationLogger))
            {
                return functions;
            }

            ConnectorErrors errors = new ConnectorErrors();
            string basePath = openApiDocument.GetBasePath(errors);

            foreach (KeyValuePair<string, OpenApiPathItem> kv in openApiDocument.Paths)
            {
                string path = kv.Key;
                OpenApiPathItem ops = kv.Value;
                bool isSupportedForPath = true;
                string notSupportedReasonForPath = string.Empty;

                // Skip Webhooks
                if (ops.Extensions.Any(kvp => kvp.Key == XMsNotificationContent))
                {
                    configurationLogger?.LogInformation($"Skipping Webhook {path} {ops.Description}");
                    continue;
                }

                ValidateSupportedOpenApiPathItem(ops, ref isSupportedForPath, ref notSupportedReasonForPath, connectorSettings.FailOnUnknownExtension, configurationLogger);

                foreach (KeyValuePair<OperationType, OpenApiOperation> kv2 in ops.Operations)
                {
                    bool isSupportedForOperation = true;
                    string notSupportedReasonForOperation = string.Empty;

                    HttpMethod verb = kv2.Key.ToHttpMethod(); // "GET", "POST"...
                    OpenApiOperation op = kv2.Value;

                    if (op == null)
                    {
                        configurationLogger?.LogError($"Operation {verb} {path} is null");
                        continue;
                    }

                    // We only want to keep "actions", triggers are always ignored
                    if (op.IsTrigger())
                    {
                        configurationLogger?.LogInformation($"Operation {verb} {path} is trigger");
                        continue;
                    }

                    ValidateSupportedOpenApiOperation(op, ref isSupportedForOperation, ref notSupportedReasonForOperation, connectorSettings.FailOnUnknownExtension, configurationLogger);
                    ValidateSupportedOpenApiParameters(op, ref isSupportedForOperation, ref notSupportedReasonForOperation, connectorSettings.FailOnUnknownExtension, configurationLogger);

                    string operationName = NormalizeOperationId(op.OperationId ?? path);

                    if (string.IsNullOrEmpty(operationName))
                    {
                        configurationLogger?.LogError($"Operation {verb} {path}, OperationId {op.OperationId} has a null or empty operationName");
                        continue;
                    }

                    string opPath = basePath != null && basePath != "/" ? basePath + path : path;

                    if (string.IsNullOrEmpty(opPath))
                    {
                        configurationLogger?.LogError($"Operation {verb} {path}, OperationId {op.OperationId} has a null or empty operation path");
                        continue;
                    }

                    bool isSupported = isSupportedForPath && connectorIsSupported && isSupportedForOperation;
                    string notSupportedReason = !string.IsNullOrEmpty(connectorNotSupportedReason)
                                              ? connectorNotSupportedReason
                                              : !string.IsNullOrEmpty(notSupportedReasonForPath)
                                              ? notSupportedReasonForPath
                                              : notSupportedReasonForOperation;

                    ConnectorFunction connectorFunction = new ConnectorFunction(op, isSupported, notSupportedReason, operationName, opPath, verb, connectorSettings, functions, configurationLogger, globalValues)
                    {
                        Servers = openApiDocument.Servers
                    };

                    functions.Add(connectorFunction);
                }
            }

            configurationLogger?.LogInformation($"Namespace {connectorSettings.Namespace}: '{openApiDocument.Info.Title}' version {openApiDocument.Info.Version} - {functions.Count} functions found");
            configurationLogger?.LogDebug($"Functions found: {string.Join(", ", functions.Select(f => f.Name))}");

            return functions;
        }

        private static bool ValidateSupportedOpenApiDocument(OpenApiDocument openApiDocument, ref bool isSupported, ref string notSupportedReason, bool failOnUnknownExtensions, ConnectorLogger logger = null)
        {
            // OpenApiDocument - https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.models.openapidocument?view=openapi-dotnet
            // AutoRest Extensions for OpenAPI 2.0 - https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md            

            if (openApiDocument.Paths == null)
            {
                logger?.LogError($"OpenApiDocument is invalid, it has no Path");
                return false;
            }

            if (failOnUnknownExtensions)
            {
                // All these Info properties can be ignored
                // openApiDocument.Info.Description 
                // openApiDocument.Info.Version
                // openApiDocument.Info.Title
                // openApiDocument.Info.Contact
                // openApiDocument.Info.License
                // openApiDocument.Info.TermsOfService            
                List<string> infoExtensions = openApiDocument.Info.Extensions.Keys.ToList();

                // Undocumented but safe to ignore
                infoExtensions.Remove("x-ms-deployment-version");

                // Used for versioning and life cycle management of an operation.
                // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions
                infoExtensions.Remove("x-ms-api-annotation");

                // The name of the API
                // https://www.ibm.com/docs/en/api-connect/5.0.x?topic=reference-api-connect-context-variables
                infoExtensions.Remove("x-ibm-name");

                // Custom logo image to your API reference documentation
                // https://redocly.com/docs/api-reference-docs/specification-extensions/x-logo/
                infoExtensions.Remove("x-logo");

                // Undocumented but safe to ignore
                infoExtensions.Remove("x-ms-connector-name");
                infoExtensions.Remove("x-ms-keywords");

                if (infoExtensions.Count != 0)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiDocument Info contains unsupported extensions {string.Join(", ", infoExtensions)}";
                    logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                }
            }

            // openApiDocument.ExternalDocs - may contain URL pointing to doc

            if (openApiDocument.Components != null)
            {
                if (isSupported && openApiDocument.Components.Callbacks.Any())
                {
                    // Callback Object: A map of possible out-of band callbacks related to the parent operation.
                    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.models.openapicallback
                    isSupported = false;
                    notSupportedReason = $"OpenApiDocument Components contains Callbacks";
                    logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                }

                // openApiDocument.Examples can be ignored

                if (isSupported && failOnUnknownExtensions)
                {
                    if (openApiDocument.Components.Extensions.Any())
                    {
                        isSupported = false;
                        notSupportedReason = $"OpenApiDocument Components contains Extensions {string.Join(", ", openApiDocument.Components.Extensions.Keys)}";
                        logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                    }
                }

                if (isSupported && openApiDocument.Components.Headers.Any())
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiDocument Components contains Headers";
                    logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                }

                if (isSupported && openApiDocument.Components.Links.Any())
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiDocument Components contains Links";
                    logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                }

                // openApiDocument.Components.Parameters is ok                
                // openApiDocument.Components.RequestBodies is ok
                // openApiDocument.Components.Responses contains references from "path" definitions
                // openApiDocument.Components.Schemas contains global "definitions"

                if (openApiDocument.Components.SecuritySchemes.Count > 0)
                {
                    logger?.LogInformation($"Unsupported document: {notSupportedReason}");
                }
            }

            if (isSupported && failOnUnknownExtensions)
            {
                List<string> extensions = openApiDocument.Extensions.Where(e => !((e.Value is IList<IOpenApiAny> oaa && oaa.Count == 0) || (e.Value is OpenApiObject oao && oao.Count == 0))).Select(e => e.Key).ToList();

                // Only metadata that can be ignored
                // https://learn.microsoft.com/en-us/connectors/custom-connectors/certification-submission
                extensions.Remove("x-ms-connector-metadata");

                // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-capabilities
                extensions.Remove("x-ms-capabilities");

                // Undocumented but only contains URL and description
                extensions.Remove("x-ms-docs");

                if (extensions.Count != 0)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiDocument contains unsupported Extensions {string.Join(", ", extensions)}";
                    logger?.LogWarning($"Unsupported document: {notSupportedReason}");
                }
            }

            // openApiDocument.ExternalDocs - can be ignored
            // openApiDocument.SecurityRequirements - can be ignored as we don't manage this part        
            // openApiDocument.Tags - can be ignored

            if (isSupported && openApiDocument.Workspace != null)
            {
                isSupported = false;
                notSupportedReason = $"OpenApiDocument contains unsupported Workspace";
                logger?.LogWarning($"Unsupported document: {notSupportedReason}");
            }

            return true;
        }

        private static void ValidateSupportedOpenApiPathItem(OpenApiPathItem ops, ref bool isSupported, ref string notSupportedReason, bool failOnUnknownExtensions, ConnectorLogger logger = null)
        {
            if (failOnUnknownExtensions)
            {
                List<string> pathExtensions = ops.Extensions.Keys.ToList();

                // Can safely be ignored
                pathExtensions.Remove("x-summary");

                if (pathExtensions.Count != 0)
                {
                    // x-swagger-router-controller not supported - https://github.com/swagger-api/swagger-inflector#development-lifecycle                                
                    // x-ms-notification - https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-notification-content
                    isSupported = false;
                    notSupportedReason = $"OpenApiPathItem contains unsupported Extensions {string.Join(", ", ops.Extensions.Keys)}";
                    logger?.LogWarning($"Unsupported path '{ops.Description}': {notSupportedReason}");
                }
            }
        }

        private static void ValidateSupportedOpenApiOperation(OpenApiOperation op, ref bool isSupported, ref string notSupportedReason, bool failOnUnknownExtensions, ConnectorLogger logger = null)
        {
            if (!isSupported)
            {
                return;
            }

            if (op.Callbacks.Any())
            {
                isSupported = false;
                notSupportedReason = $"OpenApiOperation contains unsupported Callbacks";
                logger?.LogWarning($"Unsupported operationId {op.OperationId}: {notSupportedReason}");
            }

            if (failOnUnknownExtensions)
            {
                List<string> opExtensions = op.Extensions.Keys.ToList();

                // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions
                opExtensions.Remove(XMsVisibility);
                opExtensions.Remove(XMsSummary);
                opExtensions.Remove(XMsExplicitInput);
                opExtensions.Remove(XMsDynamicValues);
                opExtensions.Remove(XMsDynamicSchema);
                opExtensions.Remove(XMsDynamicProperties);
                opExtensions.Remove(XMsMediaKind);
                opExtensions.Remove(XMsDynamicList);
                opExtensions.Remove(XMsRequireUserConfirmation);
                opExtensions.Remove("x-ms-api-annotation");
                opExtensions.Remove("x-ms-no-generic-test");

                // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-capabilities
                opExtensions.Remove("x-ms-capabilities");

                // https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-pageable
                opExtensions.Remove(XMsPageable);

                opExtensions.Remove("x-ms-test-value");
                opExtensions.Remove(XMsUrlEncoding);
                opExtensions.Remove("x-ms-openai-data");

                // Not supported x-ms-no-generic-test - Present in https://github.com/microsoft/PowerPlatformConnectors but not documented
                // Other not supported extensions:
                //   x-components, x-generator, x-ms-openai-data, x-ms-docs, x-servers

                if (isSupported && opExtensions.Count != 0)
                {
                    isSupported = false;

                    // x-ms-pageable not supported - https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-pageable
                    notSupportedReason = $"OpenApiOperation contains unsupported Extensions {string.Join(", ", opExtensions)}";
                    logger?.LogWarning($"OperationId {op.OperationId}: {notSupportedReason}");
                }
            }
        }

        private static void ValidateSupportedOpenApiParameters(OpenApiOperation op, ref bool isSupported, ref string notSupportedReason, bool failOnUnknownExtensions, ConnectorLogger logger = null)
        {
            foreach (OpenApiParameter param in op.Parameters)
            {
                // param.AllowEmptyValue unused

                if (param == null)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter is null";
                    logger?.LogWarning($"OperationId {op.OperationId} has a null OpenApiParameter");
                    return;
                }

                if (param.Deprecated)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} is deprecated";
                    logger?.LogWarning($"OperationId {op.OperationId}, parameter {param.Name} is deprecated");
                    return;
                }

                if (param.AllowReserved)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported AllowReserved";
                    logger?.LogWarning($"OperationId {op.OperationId}, parameter {param.Name}: {notSupportedReason}");
                    return;
                }

                if (param.Content.Any())
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported Content {string.Join(", ", param.Content.Keys)}";
                    logger?.LogWarning($"OperationId {op.OperationId}, parameter {param.Name}: {notSupportedReason}");
                    return;
                }

                // param.Explode

                if (param.Style != null && param.Style != ParameterStyle.Simple && param.Style != ParameterStyle.Form)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported Style";
                    logger?.LogWarning($"OperationId {op.OperationId}, parameter {param.Name}: {notSupportedReason}");
                    return;
                }
            }
        }

        // Parse an OpenApiDocument and return functions. 
        internal static (List<ConnectorFunction> connectorFunctions, List<ConnectorTexlFunction> texlFunctions) ParseInternal(ConnectorSettings connectorSettings, OpenApiDocument openApiDocument, ConnectorLogger configurationLogger = null, IReadOnlyDictionary<string, FormulaValue> globalValues = null)
        {
            List<ConnectorFunction> cFunctions = GetFunctionsInternal(connectorSettings, openApiDocument, configurationLogger, globalValues).Where(f => ShouldIncludeFunction(f, connectorSettings)).ToList();
            List<ConnectorTexlFunction> tFunctions = cFunctions.Select(f => new ConnectorTexlFunction(f)).ToList();

            return (cFunctions, tFunctions);
        }

        internal static string GetServer(IEnumerable<OpenApiServer> openApiServers, HttpMessageInvoker httpClient)
        {
            if (httpClient != null && httpClient is HttpClient hc)
            {
                if (hc.BaseAddress != null)
                {
                    string path = hc.BaseAddress.AbsolutePath;

                    if (path.EndsWith("/", StringComparison.Ordinal))
                    {
                        path = path.Substring(0, path.Length - 1);
                    }

                    return path;
                }

                if (hc.BaseAddress == null && openApiServers.Any())
                {
                    // descending order to prefer https
                    return openApiServers.Select(s => new Uri(s.Url)).Where(s => s.Scheme == "https").FirstOrDefault()?.OriginalString;
                }
            }

            return null;
        }
    }
}
