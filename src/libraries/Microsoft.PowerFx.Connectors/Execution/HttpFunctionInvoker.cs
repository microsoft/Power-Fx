// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    internal class HttpFunctionInvoker : FunctionInvoker<HttpMessageInvoker, HttpRequestMessage, HttpResponseMessage>
    {
        private HttpMessageInvoker HttpInvoker => _lazyHttpClient.Value;

        private readonly Lazy<HttpMessageInvoker> _lazyHttpClient;    
        
        internal ConnectorFunction Function { get; }

        public HttpFunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext)
            : base(runtimeContext)
        {
            Function = function;
            _lazyHttpClient = new Lazy<HttpMessageInvoker>(() => (HttpMessageInvoker)Context.GetInvoker(function.Namespace));                        
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        public HttpRequestMessage BuildRequest(FormulaValue[] args, CancellationToken cancellationToken)
        {                                  
            cancellationToken.ThrowIfCancellationRequested();

            // Function couldn't be initialized properly, let's stop immediately
            if (Function._internals == null)
            {
                Logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(BuildRequest)}, _function._internals is null");
                return null;
            }

            (string url, Dictionary<string, string> headers, string body) = GetRequestElements(Function, args, HttpInvoker is HttpClient hc ? hc.BaseAddress : null, cancellationToken);

            var request = new HttpRequestMessage(Function.HttpMethod, url);

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.Default, Function._internals.ContentType);
            }

            return request;
        }                

        public async Task<FormulaValue> DecodeResponseAsync(HttpResponseMessage response)
        {
            var text = response?.Content == null
                            ? string.Empty
                            : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {
                return DecodeJson(Function, text);
            }

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

        public override async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = BuildRequest(args, cancellationToken);

            if (request == null)
            {
                Logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null");
                return new ErrorValue(IRContext.NotInSource(Function.ReturnType), new ExpressionError()
                {
                    Kind = ErrorKind.Internal,
                    Severity = ErrorSeverity.Critical,
                    Message = $"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null"
                });
            }

            return await ExecuteHttpRequest(request, HttpInvoker, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<FormulaValue> InvokeAsync(string nextLink, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = new HttpRequestMessage(Function.HttpMethod, new Uri(nextLink).PathAndQuery);
            return await ExecuteHttpRequest(request, HttpInvoker, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FormulaValue> ExecuteHttpRequest(HttpRequestMessage request, HttpMessageInvoker httpInvoker, CancellationToken cancellationToken)
        {
            var client = httpInvoker ?? HttpInvoker;
            var response = await SendAsync(client, request, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode >= 300)
            {
                Logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(ExecuteHttpRequest)}, response status code: {(int)response.StatusCode} {response.StatusCode}");
            }
            else
            {
                Logger?.LogInformation($"In {nameof(HttpFunctionInvoker)}.{nameof(ExecuteHttpRequest)}, response status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            return await DecodeResponseAsync(response).ConfigureAwait(false);
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpMessageInvoker invoker, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }    
}
