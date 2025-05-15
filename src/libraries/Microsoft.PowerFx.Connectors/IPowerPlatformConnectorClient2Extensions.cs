// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    public static class IPowerPlatformConnectorClient2Extensions
    {
        /// <summary>
        /// Sends an HTTP request to the Power Platform connector.
        /// </summary>
        /// <param name="client">Client.</param>
        /// <param name="requestMessage">HTTP request message. URI path will be used as a relative path for the connector request.</param>
        /// <param name="diagnosticOptions">Diagnostic options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="HttpResponseMessage"/>.</returns>
        public static Task<HttpResponseMessage> SendAsync(
            this IPowerPlatformConnectorClient2 client,
            HttpRequestMessage requestMessage,
            PowerPlatformConnectorClient2DiagnosticOptions diagnosticOptions,
            CancellationToken cancellationToken)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            return client.SendAsync(
                requestMessage.Method,
                requestMessage.RequestUri.PathAndQuery,
                requestMessage.Headers,
                requestMessage.Content,
                diagnosticOptions,
                cancellationToken);
        }
    }
}
