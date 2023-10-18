// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    internal class HttpFunctionInvoker : FunctionInvoker
    {        
        internal readonly HttpMessageInvoker _invoker;

        public HttpFunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext, HttpMessageInvoker invoker)
            : base(function, runtimeContext)
        {            
            _invoker = invoker;
        }

        public override async Task<FormulaValue> SendAsync(InvokerParameters invokerParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(Function.HttpMethod, invokerParameters.Url);

            foreach (var kv in invokerParameters.Headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (invokerParameters.Body != null)
            {
                request.Content = new StringContent(invokerParameters.Body, Encoding.Default, invokerParameters.ContentType);
            }

            HttpResponseMessage response = await _invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {
                Logger?.LogInformation($"In {nameof(HttpFunctionInvoker)}.{nameof(SendAsync)}, response status code: {statusCode} {response.StatusCode}");
                return DecodeJson(Function, Context.ReturnRawResults, text);
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
    }
}
