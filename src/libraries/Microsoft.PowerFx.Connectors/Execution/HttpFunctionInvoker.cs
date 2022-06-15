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
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    internal class HttpFunctionInvoker
    {
        private readonly HttpMessageInvoker _httpClient;
        private readonly HttpMethod _method;
        private readonly string _path;
        private readonly FormulaType _returnType;
        private readonly ArgumentMapper _argMapper;
        private readonly ICachingHttpClient _cache;

        public HttpFunctionInvoker(
            HttpMessageInvoker httpClient,
            HttpMethod method,
            string path,
            FormulaType returnType,
            ArgumentMapper argMapper,
            ICachingHttpClient cache = null)
        {
            _httpClient = httpClient;
            _method = method;
            _path = path;
            _argMapper = argMapper;
            _cache = cache ?? NonCachingClient.Instance;
            _returnType = returnType;
        }

        internal static void VerifyCanHandle(FxParameterLocation? location)
        {
            switch (location.Value)
            {
                case FxParameterLocation.Path:
                case FxParameterLocation.Query:
                case FxParameterLocation.Header:
                case FxParameterLocation.Body:
                    break;

                case FxParameterLocation.Cookie:
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
            string body = null;

            var map = _argMapper.ConvertToSwagger(args);
            foreach (var param in _argMapper.OpenApiParameters)
            {
                if (param.In == FxParameterLocation.Body && param.Schema.Properties != null && param.Schema.Properties.Any())
                {
                    var props = new Dictionary<string, object>();

                    foreach (var property in param.Schema.Properties)
                    {
                        if (map.TryGetValue(property.Key, out var paramValue))
                        {
                            props.Add(property.Key, paramValue.ToObject());
                        }
                    }

                    body = _argMapper.ContentType switch
                    {
                       "application/x-www-form-urlencoded" => props.ToFormUrlEncoded(),
                       "application/xml" => props.ToXml(param.Schema.Reference.Id ?? "Xml"),
                       _ => props.ToJson()
                    };                    
                }
                else if (map.TryGetValue(param.Name, out var paramValue))
                {
                    var valueStr = paramValue.ToObject().ToString();

                    switch (param.In.Value)
                    {
                        case FxParameterLocation.Path:
                            path = path.Replace("{" + param.Name + "}", HttpUtility.UrlEncode(valueStr));
                            break;

                        case FxParameterLocation.Query:
                            query.Append((query.Length == 0) ? "?" : "&");
                            query.Append(param.Name);
                            query.Append('=');
                            query.Append(HttpUtility.UrlEncode(valueStr));
                            break;

                        case FxParameterLocation.Header:
                            headers.Add(param.Name, valueStr);
                            break;

                        case FxParameterLocation.Body:
                            body = paramValue.ToObject().ToString();
                            break;

                        case FxParameterLocation.Cookie:
                        default:
                            throw new NotImplementedException($"{param.In}");
                    }
                }
            }

            var url = path + query.ToString();

            // $$$ Not handling Body yet...
            var request = new HttpRequestMessage(_method, url);            

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (!string.IsNullOrEmpty(body))
            {                
                request.Content = new StringContent(body, Encoding.Default, _argMapper.ContentType);                
            }

            return request;
        }

        public async Task<FormulaValue> DecodeResponseAsync(HttpResponseMessage response)
        {
            // $$$ Do we need to check response media type to confirm that the content is indeed json?
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var msg = $"Connector call failed {response.StatusCode}): " + json;

                // $$$ Do any connectors have 40x behavior here in their response code?
                // or 201 long-ops behavior?

                // $$$ Still type this. 
                return FormulaValue.NewError(new ExpressionError
                {
                    Kind = ErrorKind.Unknown,
                    Message = msg
                });
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return FormulaValue.NewBlank(_returnType);
            }

            // $$$ Proper marshalling?,  use _returnType;
            // If schema was an array, we returned a Single Column Table type for it. 
            // Need to ensure we marshal it consistency here. 
            var result = FormulaValue.FromJson(json);

            return result;
        }

        public async Task<FormulaValue> InvokeAsync(string cacheScope, CancellationToken cancel, FormulaValue[] args)
        {
            FormulaValue result;
            var request = BuildRequest(args);

            var key = request.RequestUri.ToString();

            if (request.Method != HttpMethod.Get)
            {
                _cache.Reset(cacheScope);
                key = null; // don't bother caching
            }

            var result2 = await _cache.TryGetAsync(cacheScope, key, async () =>
            {
                var response = await _httpClient.SendAsync(request, cancel);
                result = await DecodeResponseAsync(response);
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

        public ScopedHttpFunctionInvoker(string cacheScope, HttpFunctionInvoker invoker)
        {
            _cacheScope = cacheScope;
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        }

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            return _invoker.InvokeAsync(_cacheScope, cancel, args);
        }
    }
}
