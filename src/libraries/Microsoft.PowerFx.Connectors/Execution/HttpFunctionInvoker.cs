// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    public class HttpFunctionInvoker : FunctionInvoker
    {        
        internal readonly HttpMessageInvoker _invoker;
        internal readonly bool _rawResults;

        public HttpFunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext, bool rawResults, HttpMessageInvoker invoker)
            : base(function, runtimeContext)
        {            
            _invoker = invoker;
            _rawResults = rawResults;
        }

        public override async Task<FormulaValue> SendAsync(InvokerParameters invokerParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, string> headers = invokerParameters.HeaderParameters != null ? invokerParameters.HeaderParameters.ToDictionary(hp => hp.Name, hp => GetValue(hp), StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>();
            string body = GetBody(Function, Function._internals.BodySchemaReferenceId, Function._internals.SchemaLessBody, invokerParameters.BodyParameters, cancellationToken);
            string path = invokerParameters.PathParameters?.Any() == true ? invokerParameters.PathParameters.Aggregate(Function.OperationPath, (p, ip) => p.Replace("{" + ip.Name + "}", GetHttpEncodedValue(ip))) : Function.OperationPath;
            string query = invokerParameters.QueryParameters?.Any() == true ? "?" + string.Join("&", invokerParameters.QueryParameters.Select(p => $"{p.Name}={GetHttpEncodedValue(p)}")) : string.Empty;
            var url = (OpenApiParser.GetServer(Function.Servers, invokerParameters.Address) ?? string.Empty) + path + query;

            using var request = new HttpRequestMessage(Function.HttpMethod, url);

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.Default, invokerParameters.ContentType);
            }

            HttpResponseMessage response = await _invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {
                Logger?.LogInformation($"In {nameof(HttpFunctionInvoker)}.{nameof(SendAsync)}, response status code: {statusCode} {response.StatusCode}");
                return DecodeJson(Function, _rawResults, text);
            }

            Logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(SendAsync)}, response status code: {statusCode} {response.StatusCode}");

            if (ThrowOnError)
            {
                throw new HttpRequestException($"Http Status Error {statusCode}: {text}");
            }

            return FormulaValue.NewError(
                    new ExpressionError()
                    {
                        Kind = ErrorKind.Network,
                        Severity = ErrorSeverity.Critical,
                        Message = $"The server returned an HTTP error with code {statusCode}. Response: {text}"
                    },
                    Function.ReturnType);
        }

        private string GetHttpEncodedValue(InvokerParameter param)
        {
            string valueStr = GetValue(param);

            valueStr = HttpUtility.UrlEncode(valueStr);

            if (param.DoubleEncoded)
            {
                valueStr = HttpUtility.UrlEncode(valueStr);
            }

            return valueStr;
        }

        private string GetValue(InvokerParameter param) => param.Value?.ToObject()?.ToString() ?? string.Empty;

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        private string GetBody(ConnectorFunction function, string referenceId, bool schemaLessBody, IReadOnlyList<InvokerParameter> bodyParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (bodyParameters == null || !bodyParameters.Any())
            {
                return null;
            }

            FormulaValueSerializer serializer = null;
            try
            {
                serializer = function._internals.ContentType.ToLowerInvariant() switch
                {
                    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(UtcConverter, schemaLessBody),
                    OpenApiExtensions.ContentType_TextPlain => new OpenApiTextSerializer(UtcConverter, schemaLessBody),
                    _ => new OpenApiJsonSerializer(UtcConverter, schemaLessBody)
                };

                serializer.StartSerialization(referenceId);
                foreach (InvokerParameter param in bodyParameters)
                {
                    serializer.SerializeValue(param.Name, param.Schema, param.Value);
                }

                serializer.EndSerialization();

                return serializer.GetResult();
            }
            finally
            {
                if (serializer != null && serializer is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }
    }
}
