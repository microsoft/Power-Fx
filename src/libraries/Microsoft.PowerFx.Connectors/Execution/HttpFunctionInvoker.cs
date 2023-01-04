// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    internal class HttpFunctionInvoker
    {
        private readonly HttpMessageInvoker _httpClient;
        private readonly HttpMethod _method;
        private readonly string _path;
        internal readonly FormulaType _returnType;
        private readonly ArgumentMapper _argMapper;
        private readonly ICachingHttpClient _cache;

        public HttpFunctionInvoker(HttpMessageInvoker httpClient, HttpMethod method, string path, FormulaType returnType, ArgumentMapper argMapper, ICachingHttpClient cache = null)
        {
            _httpClient = httpClient;
            _method = method;
            _path = path;
            _argMapper = argMapper;
            _cache = cache ?? NonCachingClient.Instance;
            _returnType = returnType;
        }

        internal static void VerifyCanHandle(ParameterLocation? location)
        {
            switch (location.Value)
            {
                case ParameterLocation.Path:
                case ParameterLocation.Query:
                case ParameterLocation.Header:
                    break;

                case ParameterLocation.Cookie:
                default:
                    throw new NotImplementedException($"Unsupported ParameterIn {location}");
            }
        }

        public HttpRequestMessage BuildRequest(FormulaValue[] args)
        {
            var path = _path;
            var query = new StringBuilder();

            // https://stackoverflow.com/questions/5258977/are-http-headers-case-sensitive
            // Header names are not case sensitive.
            // From RFC 2616 - "Hypertext Transfer Protocol -- HTTP/1.1", Section 4.2, "Message Headers"
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HttpContent body = null;
            Dictionary<string, (OpenApiSchema, FormulaValue)> bodyParts = new ();

            var map = _argMapper.ConvertToSwagger(args);

            foreach (var param in _argMapper.OpenApiBodyParameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    bodyParts.Add(param.Name, (param.Schema, paramValue));
                }
            }

            if (bodyParts.Any())
            {
                body = GetBody(_argMapper.ReferenceId, _argMapper.SchemaLessBody, bodyParts);
            }

            foreach (var param in _argMapper.OpenApiParameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    var valueStr = paramValue.ToObject().ToString();

                    switch (param.In.Value)
                    {
                        case ParameterLocation.Path:
                            path = path.Replace("{" + param.Name + "}", HttpUtility.UrlEncode(valueStr));
                            break;

                        case ParameterLocation.Query:
                            query.Append((query.Length == 0) ? "?" : "&");
                            query.Append(param.Name);
                            query.Append('=');
                            query.Append(HttpUtility.UrlEncode(valueStr));
                            break;

                        case ParameterLocation.Header:
                            headers.Add(param.Name, valueStr);
                            break;

                        case ParameterLocation.Cookie:
                        default:
                            throw new NotImplementedException($"{param.In}");
                    }
                }
            }

            var url = path + query.ToString();
            var request = new HttpRequestMessage(_method, url);

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = body;
            }

            return request;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        private HttpContent GetBody(string referenceId, bool schemaLessBody, Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> map)
        {
            FormulaValueSerializer serializer = null;

            try
            {
                serializer = _argMapper.ContentType.ToLowerInvariant() switch
                {
                    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(schemaLessBody),
                    OpenApiExtensions.ContentType_TextPlain => new OpenApiTextSerializer(schemaLessBody),
                    _ => new OpenApiJsonSerializer(schemaLessBody)
                };

                serializer.StartSerialization(referenceId);
                foreach (var kv in map)
                {
                    serializer.SerializeValue(kv.Key, kv.Value.Schema, kv.Value.Value);
                }

                serializer.EndSerialization();

                return new StringContent(serializer.GetResult(), Encoding.Default, _argMapper.ContentType);
            }
            finally
            {
                if (serializer != null && serializer is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        public async Task<FormulaValue> DecodeResponseAsync(HttpResponseMessage response, FormulaType returnType)
        {
            var text = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {                
                return string.IsNullOrWhiteSpace(text) 
                    ? FormulaValue.NewBlank(_returnType) 
                    : FormulaValueJSON.FromJson(text); // $$$ Do we need to check response media type to confirm that the content is indeed json?
            }

            return FormulaValue.NewError(
                    new ExpressionError()
                    {
                        Kind = ErrorKind.Network,
                        Severity = ErrorSeverity.Critical,
                        Message = $"The server returned an HTTP error with code {statusCode}."
                    },
                    returnType);
        }

        public async Task<FormulaValue> InvokeAsync(string cacheScope, FormulaValue[] args, CancellationToken cancellationToken)
        {
            FormulaValue result;
            using var request = BuildRequest(args);

            var key = request.RequestUri.ToString();

            if (request.Method != HttpMethod.Get)
            {
                _cache.Reset(cacheScope);
                key = null; // don't bother caching
            }

            var result2 = await _cache.TryGetAsync(cacheScope, key, async () =>
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                result = await DecodeResponseAsync(response, _returnType);
                return result;
            });

            return result2;
        }
    }

    // Closure over a HttpFunctionInvoker, but scoped to a cacheScope.
    internal class ScopedHttpFunctionInvoker : IAsyncTexlFunction
    {
        private readonly string _cacheScope;
        private readonly HttpFunctionInvoker _invoker;

        public ScopedHttpFunctionInvoker(DPath ns, string name, string cacheScope, HttpFunctionInvoker invoker)
        {
            Namespace = ns;
            Name = name;

            _cacheScope = cacheScope;
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        }

        public DPath Namespace { get; }

        public string Name { get; }

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            return _invoker.InvokeAsync(_cacheScope, args, cancellationToken);
        }
    }
}
