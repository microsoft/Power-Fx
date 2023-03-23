// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using Microsoft.AppMagic.Authoring;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
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
        public static IEnumerable<ConnectorFunction> GetFunctions(OpenApiDocument openApiDocument)
        {
            if (openApiDocument == null)
            {
                throw new ArgumentNullException(nameof(openApiDocument));
            }

            if (openApiDocument.Paths == null)
            {
                throw new InvalidOperationException($"OpenApiDocument is invalid - has null paths");
            }

            List<ConnectorFunction> functions = new ();
            string basePath = openApiDocument.GetBasePath();

            foreach (KeyValuePair<string, OpenApiPathItem> kv in openApiDocument.Paths)
            {
                string path = kv.Key;
                OpenApiPathItem ops = kv.Value;

                foreach (KeyValuePair<OperationType, OpenApiOperation> kv2 in ops.Operations) 
                {
                    HttpMethod verb = kv2.Key.ToHttpMethod(); // "GET", "POST"
                    OpenApiOperation op = kv2.Value;

                    // We only want to keep "actions"
                    if (op.IsTrigger())
                    {
                        continue;
                    }
                    
                    string operationName = NormalizeOperationId(op.OperationId) ?? path.Replace("/", string.Empty);
                    string opPath = basePath != null ? basePath + path : path;

                    functions.Add(new ConnectorFunction(op, operationName, opPath, verb));            
                }
            }

            return functions;
        }

        // Parse an OpenApiDocument and return functions. 
        internal static List<ServiceFunction> Parse(string functionNamespace, OpenApiDocument openApiDocument, HttpMessageInvoker httpClient = null, ICachingHttpClient cache = null)
        {
            if (openApiDocument == null)
            {
                throw new ArgumentNullException(nameof(openApiDocument));
            }

            if (string.IsNullOrWhiteSpace(functionNamespace))
            {
                throw new ArgumentException(nameof(functionNamespace));
            }

            List<ServiceFunction> newFunctions = new List<ServiceFunction>();
            string basePath = openApiDocument.GetBasePath();
            DPath theNamespace = DPath.Root.Append(new DName(functionNamespace));

            if (openApiDocument.Paths == null)
            {
                // OpenAPI spec: Paths is a required parameter
                throw new InvalidOperationException($"OpenApiDocument is invalid - has null paths");
            }

            foreach (var kv in openApiDocument.Paths)
            {
                string path = kv.Key;
                OpenApiPathItem ops = kv.Value;

                foreach (var kv2 in ops.Operations)
                {
                    HttpMethod verb = kv2.Key.ToHttpMethod(); // "GET", "POST"
                    OpenApiOperation op = kv2.Value;

                    if (op.IsTrigger())
                    {
                        continue;
                    }

                    // We need to remove invalid chars to be consistent with Power Apps
                    string operationName = NormalizeOperationId(op.OperationId) ?? path.Replace("/", string.Empty);

                    FormulaType returnType = op.GetReturnType();
                    string opPath = basePath != null && basePath != "/" ? basePath + path : path;
                    ArgumentMapper argMapper = new ArgumentMapper(op.Parameters, op);
                    IAsyncTexlFunction invoker = null;

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

                    ServiceFunction sfunc = new ServiceFunction(
                        null,
                        theNamespace,
                        operationName,
                        operationName,
                        description, // Template.GetFunctionDescription(funcTemplate.Name),
                        returnType._type,
                        BigInteger.Zero,
                        argMapper.ArityMin,
                        argMapper.ArityMax,
                        isBehavior,
                        isAutoRefreshable,
                        isDynamic,
                        isCacheEnabled,
                        cacheTimeoutMs,
                        isHidden,
                        parameterOptions,
                        argMapper.OptionalParamInfo,
                        argMapper.RequiredParamInfo,
                        parameterDefaultValues,                        
                        "action", //  funcTemplate.ActionName,??                        
                        argMapper._parameterTypes)
                    {
                        _invoker = invoker
                    };

                    newFunctions.Add(sfunc);
                }
            }

            // post processing for ConnectorDynamicValue, identify service functions
            foreach (ServiceFunction sf in newFunctions)
            {
                foreach (ServiceFunctionParameterTemplate sfpt in sf._requiredParameters)
                {
                    if (sfpt.ConnectorDynamicValue != null)
                    {
                        sfpt.ConnectorDynamicValue.ServiceFunction = newFunctions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicValue.OperationId);
                    }

                    if (sfpt.ConnectorDynamicSchema != null)
                    {
                        sfpt.ConnectorDynamicSchema.ServiceFunction = newFunctions.FirstOrDefault(f => f.Name == sfpt.ConnectorDynamicSchema.OperationId);
                    }
                }
            }

            return newFunctions;
        }
       
        internal static bool IsSafeHttpMethod(HttpMethod httpMethod)
        {
            // HTTP/1.1 spec states that only GET and HEAD requests are 'safe' by default.
            // https://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html
            return httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Head;
        }
    }
}
