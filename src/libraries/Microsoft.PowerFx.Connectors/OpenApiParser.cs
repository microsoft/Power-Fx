// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using Microsoft.AppMagic.Authoring;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.OpenApiHelperFunctions;

namespace Microsoft.PowerFx.Connectors
{
    public class OpenApiParser
    {
        public static IEnumerable<ConnectorFunction> GetFunctions(OpenApiDocument openApiDocument, HttpClient httpClient = null, bool throwOnError = false, bool numberIsFloat = false)
        {
            ValidateSupportedOpenApiDocument(openApiDocument);

            List<ConnectorFunction> functions = new ();
            List<ServiceFunction> sFunctions = new ();
            string basePath = openApiDocument.GetBasePath();

            foreach (KeyValuePair<string, OpenApiPathItem> kv in openApiDocument.Paths)
            {
                string path = kv.Key;
                OpenApiPathItem ops = kv.Value;
                bool isSupported = true;
                string notSupportedReason = string.Empty;

                // ops.Description

                ValidateSupportedOpenApiPathItem(ops, ref isSupported, ref notSupportedReason);

                foreach (KeyValuePair<OperationType, OpenApiOperation> kv2 in ops.Operations)
                {
                    HttpMethod verb = kv2.Key.ToHttpMethod(); // "GET", "POST"...
                    OpenApiOperation op = kv2.Value;

                    // We only want to keep "actions", triggers are always ignored
                    if (op.IsTrigger())
                    {
                        continue;
                    }

                    ValidateSupportedOpenApiOperation(op, ref isSupported, ref notSupportedReason);
                    ValidateSupportedOpenApiParameters(op, ref isSupported, ref notSupportedReason);

                    string operationName = NormalizeOperationId(op.OperationId) ?? path.Replace("/", string.Empty);
                    string opPath = basePath != null ? basePath + path : path;
                    ConnectorFunction connectorFunction = new ConnectorFunction(op, isSupported, notSupportedReason, operationName, opPath, verb, httpClient: httpClient, throwOnError: throwOnError, numberIsFloat: numberIsFloat);

                    functions.Add(connectorFunction);
                    sFunctions.Add(connectorFunction._defaultServiceFunction);
                }
            }

            // post processing for ConnectorDynamicValue, identify service functions
            foreach (ConnectorFunction cf in functions)
            {
                if (cf._defaultServiceFunction != null)
                {
                    foreach (ServiceFunctionParameterTemplate sfpt in cf._defaultServiceFunction._requiredParameters)
                    {
                        if (sfpt.ConnectorDynamicValue != null)
                        {
                            sfpt.ConnectorDynamicValue.ServiceFunction = sFunctions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicValue.OperationId);
                        }

                        if (sfpt.ConnectorDynamicSchema != null)
                        {
                            sfpt.ConnectorDynamicSchema.ServiceFunction = sFunctions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicSchema.OperationId);
                        }
                    }
                }
            }

            return functions;
        }

        private static void ValidateSupportedOpenApiDocument(OpenApiDocument openApiDocument)
        {
            // OpenApiDocument - https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.models.openapidocument?view=openapi-dotnet
            // AutoRest Extensions for OpenAPI 2.0 - https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md

            if (openApiDocument == null)
            {
                throw new ArgumentNullException(nameof(openApiDocument));
            }

            if (openApiDocument.Paths == null)
            {
                throw new InvalidOperationException($"OpenApiDocument is invalid - has null paths");
            }

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

            if (infoExtensions.Any())
            {
                throw new NotImplementedException($"OpenApiDocument Info contains unsupported extensions {string.Join(", ", infoExtensions)}");
            }

            // openApiDocument.ExternalDocs - may contain URL pointing to doc
            if (openApiDocument.Components != null)
            {
                if (openApiDocument.Components.Callbacks.Any())
                {
                    // Callback Object: A map of possible out-of band callbacks related to the parent operation.
                    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.models.openapicallback
                    throw new NotImplementedException($"OpenApiDocument Components contains Callbacks");
                }

                // openApiDocument.Examples can be ignored

                if (openApiDocument.Components.Extensions.Any())
                {
                    throw new NotImplementedException($"OpenApiDocument Components contains Extensions");
                }

                if (openApiDocument.Components.Headers.Any())
                {
                    throw new NotImplementedException($"OpenApiDocument Components contains Headers");
                }

                if (openApiDocument.Components.Links.Any())
                {
                    throw new NotImplementedException($"OpenApiDocument Components contains Links");
                }

                // openApiDocument.Components.Parameters is ok                
                // openApiDocument.Components.RequestBodies is ok
                // openApiDocument.Components.Responses contains references from "path" definitions
                // openApiDocument.Components.Schemas contains global "definitions"
                // openApiDocument.Components.SecuritySchemes are critical but as we don't manage them at all, we'll ignore this parameter                
            }

            List<string> extensions = openApiDocument.Extensions.Where(e => !((e.Value is OpenApiArray oaa && oaa.Count == 0) || (e.Value is OpenApiObject oao && oao.Count == 0))).Select(e => e.Key).ToList();

            // Only metadata that can be ignored
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/certification-submission
            extensions.Remove("x-ms-connector-metadata");

            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-capabilities
            extensions.Remove("x-ms-capabilities");

            // Undocumented but only contains URL and description
            extensions.Remove("x-ms-docs");
            
            if (extensions.Any())
            {                
                throw new NotImplementedException($"OpenApiDocument contains unsupported Extensions {string.Join(", ", extensions)}");
            }

            // openApiDocument.ExternalDocs - can be ignored
            // openApiDocument.SecurityRequirements - can be ignored as we don't manage this part        
            // openApiDocument.Tags - can be ignored

            if (openApiDocument.Workspace != null)
            {
                throw new NotImplementedException($"OpenApiDocument contains unsupported Workspace");
            }
        }

        private static void ValidateSupportedOpenApiPathItem(OpenApiPathItem ops, ref bool isSupported, ref string notSupportedReason)
        {
            if (!isSupported)
            {
                return;
            }

            List<string> pathExtensions = ops.Extensions.Keys.ToList();

            // Can safely be ignored
            pathExtensions.Remove("x-summary");

            if (pathExtensions.Any())
            {
                // x-swagger-router-controller not supported - https://github.com/swagger-api/swagger-inflector#development-lifecycle                                
                // x-ms-notification - https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-notification-content
                isSupported = false;
                notSupportedReason = $"OpenApiPathItem contains unsupported Extensions {string.Join(", ", ops.Extensions.Keys)}";
            }
        }

        private static void ValidateSupportedOpenApiOperation(OpenApiOperation op, ref bool isSupported, ref string notSupportedReason)
        {
            if (!isSupported)
            {
                return;
            }

            if (op.Callbacks.Any())
            {
                isSupported = false;
                notSupportedReason = $"OpenApiOperation contains unsupported Callbacks";
            }

            if (isSupported && op.Deprecated)
            {
                isSupported = false;
                notSupportedReason = $"OpenApiOperation is deprecated";
            }

            List<string> opExtensions = op.Extensions.Keys.ToList();

            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions
            opExtensions.Remove("x-ms-visibility");
            opExtensions.Remove("x-ms-summary");
            opExtensions.Remove("x-ms-explicit-input");
            opExtensions.Remove("x-ms-dynamic-value");
            opExtensions.Remove("x-ms-dynamic-schema");
            opExtensions.Remove("x-ms-require-user-confirmation");
            opExtensions.Remove("x-ms-api-annotation");
            opExtensions.Remove("x-ms-no-generic-test");

            // https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-pageable
            opExtensions.Remove("x-ms-pageable");
            
            // Not supported x-ms-no-generic-test - Present in https://github.com/microsoft/PowerPlatformConnectors but not documented
            // Other not supported extensions:
            //   x-ms-notification-content, x-ms-url-encoding, x-components, x-generator, x-ms-openai-data, x-ms-docs, x-servers

            if (isSupported && opExtensions.Any())
            {
                isSupported = false;

                // x-ms-pageable not supported - https://github.com/Azure/autorest/blob/main/docs/extensions/readme.md#x-ms-pageable
                notSupportedReason = $"OpenApiOperation contains unsupported Extensions {string.Join(", ", opExtensions)}";
            }
        }

        private static void ValidateSupportedOpenApiParameters(OpenApiOperation op, ref bool isSupported, ref string notSupportedReason)
        {
            foreach (OpenApiParameter param in op.Parameters)
            {
                // param.AllowEmptyValue unused

                if (param.Deprecated)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} is deprecated";
                    return;
                }

                if (param.AllowReserved)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported AllowReserved";
                    return;
                }

                if (param.Content.Any())
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported Content {string.Join(", ", param.Content.Keys)}";
                    return;
                }

                // param.Explode

                if (param.Style != null && param.Style != ParameterStyle.Simple && param.Style != ParameterStyle.Form)
                {
                    isSupported = false;
                    notSupportedReason = $"OpenApiParameter {param.Name} contains unsupported Style";
                    return;
                }
            }
        }

        // Parse an OpenApiDocument and return functions. 
        internal static List<ServiceFunction> Parse(string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient = null, ICachingHttpClient cache = null, bool numberIsFloat = false)
        {
            if (string.IsNullOrWhiteSpace(functionNamespace))
            {
                throw new ArgumentException(nameof(functionNamespace));
            }

            ValidateSupportedOpenApiDocument(openApiDocument);

            List<ServiceFunction> functions = new List<ServiceFunction>();
            string basePath = openApiDocument.GetBasePath();
            DPath theNamespace = DPath.Root.Append(new DName(functionNamespace));

            foreach (var kv in openApiDocument.Paths)
            {
                string path = kv.Key;
                OpenApiPathItem ops = kv.Value;
                bool isSupported = true;
                string notSupportedReason = string.Empty;

                ValidateSupportedOpenApiPathItem(ops, ref isSupported, ref notSupportedReason);

                foreach (KeyValuePair<OperationType, OpenApiOperation> kv2 in ops.Operations)
                {
                    HttpMethod verb = kv2.Key.ToHttpMethod(); // "GET", "POST"
                    OpenApiOperation op = kv2.Value;

                    if (op.IsTrigger())
                    {
                        continue;
                    }

                    ValidateSupportedOpenApiOperation(op, ref isSupported, ref notSupportedReason);
                    ValidateSupportedOpenApiParameters(op, ref isSupported, ref notSupportedReason);

                    // We need to remove invalid chars to be consistent with Power Apps
                    string operationName = NormalizeOperationId(op.OperationId) ?? path.Replace("/", string.Empty);

                    FormulaType returnType = op.GetReturnType(numberIsFloat);
                    string opPath = basePath != null && basePath != "/" ? basePath + path : path;
                    ArgumentMapper argMapper = new ArgumentMapper(op.Parameters, op, numberIsFloat);
                    IAsyncTexlFunction2 invoker = null;

                    if (httpClient != null)
                    {
                        var httpInvoker = new HttpFunctionInvoker(httpClient, verb, opPath, returnType, argMapper, cache);
                        invoker = new ScopedHttpFunctionInvoker(DPath.Root.Append(DName.MakeValid(functionNamespace, out _)), operationName, functionNamespace, httpInvoker);
                    }

                    // Parameter (name,type) --> list of options. 
                    Dictionary<TypedName, List<string>> parameterOptions = new ();
                    Dictionary<string, Tuple<string, DType>> parameterDefaultValues = new (StringComparer.Ordinal);

                    bool isBehavior = !IsSafeHttpMethod(verb);
                    bool isDynamic = false;
                    bool isAutoRefreshable = false;
                    bool isCacheEnabled = false;
                    int cacheTimeoutMs = 10000;
                    bool isHidden = false;
                    string description = op.Description ?? $"Invoke {operationName}";
                    string pageLink = op.PageLink();

#pragma warning disable SA1117 // parameters should be on same line or all on different lines

                    ServiceFunction sfunc = new ServiceFunction(null, theNamespace, operationName, operationName, description, returnType._type, BigInteger.Zero, argMapper.ArityMin, argMapper.ArityMax, isBehavior, isAutoRefreshable, isDynamic, isCacheEnabled, 
                        cacheTimeoutMs, isHidden, parameterOptions, argMapper.OptionalParamInfo, argMapper.RequiredParamInfo, parameterDefaultValues, pageLink, op.Deprecated, "action", numberIsFloat, argMapper._parameterTypes)
                    {
                        _invoker = invoker
                    };

#pragma warning restore SA1117 // parameters should be on same line or all on different lines

                    functions.Add(sfunc);
                }
            }

            // post processing for ConnectorDynamicValue, identify service functions
            foreach (ServiceFunction sf in functions)
            {
                foreach (ServiceFunctionParameterTemplate sfpt in sf._requiredParameters)
                {
                    if (sfpt.ConnectorDynamicValue != null)
                    {
                        sfpt.ConnectorDynamicValue.ServiceFunction = functions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicValue.OperationId);
                    }

                    if (sfpt.ConnectorDynamicSchema != null)
                    {
                        sfpt.ConnectorDynamicSchema.ServiceFunction = functions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicSchema.OperationId);
                    }
                }
            }

            return functions;
        }

        internal static bool IsSafeHttpMethod(HttpMethod httpMethod)
        {
            // HTTP/1.1 spec states that only GET and HEAD requests are 'safe' by default.
            // https://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html
            return httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Head;
        }
    }
}
