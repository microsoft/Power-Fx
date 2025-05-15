// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Delegate for providing a bearer token for authentication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token without "Bearer" scheme as the prefix.</returns>
    public delegate Task<string> PowerPlatformConnectorClient2BearerTokenProvider(
        CancellationToken cancellationToken);

    public interface IPowerPlatformConnectorClient2
    {
        /// <summary>
        /// Sends an HTTP request to the Power Platform connector.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="operationPathAndQuery">Operation path and query string.</param>
        /// <param name="headers">Headers.</param>
        /// <param name="content">Content.</param>
        /// <param name="diagnosticOptions">Diagnostic options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="HttpResponseMessage"/>.</returns>
        Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string operationPathAndQuery,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            HttpContent content,
            PowerPlatformConnectorClient2DiagnosticOptions diagnosticOptions,
            CancellationToken cancellationToken);
    }
}
