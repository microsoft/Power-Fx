// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
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

        public HttpRequestMessage BuildRequest(FormulaValue[] args, FormattingInfo context, CancellationToken cancellationToken)
        {
            var path = _path;
            var query = new StringBuilder();

            cancellationToken.ThrowIfCancellationRequested();

            // https://stackoverflow.com/questions/5258977/are-http-headers-case-sensitive
            // Header names are not case sensitive.
            // From RFC 2616 - "Hypertext Transfer Protocol -- HTTP/1.1", Section 4.2, "Message Headers"
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HttpContent body = null;
            Dictionary<string, (OpenApiSchema, FormulaValue)> bodyParts = new();

            Dictionary<string, FormulaValue> map = _argMapper.ConvertToNamedParameters(args);

            foreach (var param in _argMapper.OpenApiBodyParameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    bodyParts.Add(param.Name, (param.Schema, paramValue));
                }
            }

            if (bodyParts.Any())
            {
                body = GetBody(_argMapper.ReferenceId, _argMapper.SchemaLessBody, bodyParts, context, cancellationToken);
            }

            foreach (var param in _argMapper.OpenApiParameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    var valueStr = paramValue?.ToObject()?.ToString() ?? string.Empty;

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
        private HttpContent GetBody(string referenceId, bool schemaLessBody, Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> map, FormattingInfo context, CancellationToken cancellationToken)
        {
            FormulaValueSerializer serializer = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                serializer = _argMapper.ContentType.ToLowerInvariant() switch
                {
                    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(context, schemaLessBody),
                    OpenApiExtensions.ContentType_TextPlain => new OpenApiTextSerializer(context, schemaLessBody),
                    _ => new OpenApiJsonSerializer(context, schemaLessBody)
                };

                serializer.StartSerialization(referenceId);
                foreach (var kv in map)
                {
                    serializer.SerializeValue(kv.Key, kv.Value.Schema, kv.Value.Value);
                }

                serializer.EndSerialization();

                string body = serializer.GetResult();
                return new StringContent(body, Encoding.Default, _argMapper.ContentType);
            }
            finally
            {
                if (serializer != null && serializer is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        public async Task<FormulaValue> DecodeResponseAsync(HttpResponseMessage response, bool throwOnError = false)
        {
            var text = response?.Content == null
                            ? string.Empty
                            : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {
                return string.IsNullOrWhiteSpace(text)
                    ? FormulaValue.NewBlank(_returnType)
                    : FormulaValueJSON.FromJson(text, _returnType); // $$$ Do we need to check response media type to confirm that the content is indeed json?
            }

            if (throwOnError)
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
                    _returnType);
        }

        public async Task<FormulaValue> InvokeAsync(FormattingInfo context, string cacheScope, FormulaValue[] args, CancellationToken cancellationToken, bool throwOnError = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = BuildRequest(args, context, cancellationToken);
            return await ExecuteHttpRequest(cacheScope, throwOnError, request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<FormulaValue> InvokeAsync(string url, string cacheScope, CancellationToken cancellationToken, bool throwOnError = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = new HttpRequestMessage(_method, new Uri(url).PathAndQuery);
            return await ExecuteHttpRequest(cacheScope, throwOnError, request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FormulaValue> ExecuteHttpRequest(string cacheScope, bool throwOnError, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri.ToString();

            if (request.Method != HttpMethod.Get)
            {
                _cache.Reset(cacheScope);
                key = null; // don't bother caching
            }

            return await _cache.TryGetAsync(cacheScope, key, async () =>
            {
                var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return await DecodeResponseAsync(response, throwOnError).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }

    // Closure over a HttpFunctionInvoker, but scoped to a cacheScope.
    internal class ScopedHttpFunctionInvoker : IAsyncTexlFunction2
    {
        private readonly string _cacheScope;
        private readonly HttpFunctionInvoker _invoker;
        private readonly bool _throwOnError;

        public ScopedHttpFunctionInvoker(DPath ns, string name, string cacheScope, HttpFunctionInvoker invoker, bool throwOnError = false)
        {
            Namespace = ns;
            Name = name;

            _cacheScope = cacheScope;
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            _throwOnError = throwOnError;
        }

        public DPath Namespace { get; }

        public string Name { get; }

        public Task<FormulaValue> InvokeAsync(FormattingInfo context, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _invoker.InvokeAsync(context, _cacheScope, args, cancellationToken, _throwOnError);
        }

        public Task<FormulaValue> InvokeAsync(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _invoker.InvokeAsync(url, _cacheScope, cancellationToken, _throwOnError);
        }
    }
}
