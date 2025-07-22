// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Client for invoking operations of Power Platform connectors.
    /// </summary>
    public class PowerPlatformConnectorClient2 : IPowerPlatformConnectorClient2
    {
        private readonly string _baseUrlStr;
        private readonly HttpMessageInvoker _httpMessageInvoker;
        private readonly PowerPlatformConnectorClient2BearerTokenProvider _tokenProvider;
        private readonly string _environmentId;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient2"/> class.
        /// </summary>
        /// <param name="document">Document used for extracting the base URL.</param>
        /// <param name="httpMessageInvoker">HTTP message invoker.</param>
        /// <param name="tokenProvider">Bearer token provider.</param>
        /// <param name="environmentId">Environment ID.</param>
        public PowerPlatformConnectorClient2(
            OpenApiDocument document,
            HttpMessageInvoker httpMessageInvoker,
            PowerPlatformConnectorClient2BearerTokenProvider tokenProvider,
            string environmentId)
            : this(GetBaseUrlFromOpenApiDocument(document), httpMessageInvoker, tokenProvider, environmentId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient2"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for requests.</param>
        /// <param name="httpMessageInvoker">HTTP message invoker.</param>
        /// <param name="tokenProvider">Bearer token provider.</param>
        /// <param name="environmentId">Environment ID.</param>
        public PowerPlatformConnectorClient2(
            Uri baseUrl,
            HttpMessageInvoker httpMessageInvoker,
            PowerPlatformConnectorClient2BearerTokenProvider tokenProvider,
            string environmentId)
        {
            this._baseUrlStr = GetBaseUrlStr(baseUrl ?? throw new ArgumentNullException(nameof(baseUrl)));
            this._httpMessageInvoker = httpMessageInvoker ?? throw new ArgumentNullException(nameof(httpMessageInvoker));
            this._tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            this._environmentId = environmentId ?? throw new ArgumentNullException(nameof(environmentId));

            static string GetBaseUrlStr(Uri uri)
            {
                var str = uri.GetLeftPart(UriPartial.Path);

                // Note (shgogna): Ensure the base URL does NOT end with "/".
                // This will allow us to concatenate the operation path to the base URL
                // without worrying about the "/".
                str = str.TrimEnd('/');
                return str;
            }
        }

        // <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string operationPathAndQuery,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            HttpContent content,
            PowerPlatformConnectorClient2DiagnosticOptions diagnosticOptions,
            CancellationToken cancellationToken)
        {
            if (operationPathAndQuery is null)
            {
                throw new ArgumentNullException(nameof(operationPathAndQuery));
            }

            var uri = this.CombineBaseUrlWithOperationPathAndQuery(operationPathAndQuery);

            if (!uri.AbsoluteUri.StartsWith(this._baseUrlStr, StringComparison.Ordinal))
            {
                throw new ArgumentException("Path traversal detected during combination of base URL path and operation path.", nameof(operationPathAndQuery));
            }

            return this.InternalSendAsync(
                method,
                uri,
                headers,
                content,
                diagnosticOptions,
                cancellationToken);
        }

        private async Task<HttpResponseMessage> InternalSendAsync(
            HttpMethod method,
            Uri uri,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            HttpContent content,
            PowerPlatformConnectorClient2DiagnosticOptions diagnosticOptions,
            CancellationToken cancellationToken)
        {
            var authToken = await this._tokenProvider(cancellationToken).ConfigureAwait(false);

            using (var req = new HttpRequestMessage(method, uri))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                this.AddDiagnosticHeaders(diagnosticOptions, req);

                foreach (var header in headers)
                {
                    req.Headers.Add(header.Key, header.Value);
                }

                req.Content = content;
                return await this._httpMessageInvoker.SendAsync(req, cancellationToken).ConfigureAwait(false);
            }
        }

        private Uri CombineBaseUrlWithOperationPathAndQuery(string operationPathAndQuery)
        {
            if (operationPathAndQuery.StartsWith("/", StringComparison.Ordinal))
            {
                return new Uri(this._baseUrlStr + operationPathAndQuery);
            }
            else
            {
                return new Uri(this._baseUrlStr + "/" + operationPathAndQuery);
            }
        }

        private void AddDiagnosticHeaders(
            PowerPlatformConnectorClient2DiagnosticOptions diagnosticOptions,
            HttpRequestMessage req)
        {
            var userAgent = string.IsNullOrWhiteSpace(diagnosticOptions?.UserAgent)
                ? $"PowerFx/{PowerPlatformConnectorClient.Version}"
                : $"{diagnosticOptions.UserAgent} PowerFx/{PowerPlatformConnectorClient.Version}";

            var clientRequestId = string.IsNullOrWhiteSpace(diagnosticOptions?.ClientRequestId)
                ? Guid.NewGuid().ToString()
                : diagnosticOptions.ClientRequestId;

            // CorrelationID can be the same as ClientRequestID
            var correlationId = string.IsNullOrWhiteSpace(diagnosticOptions?.CorrelationId)
                ? clientRequestId
                : diagnosticOptions.CorrelationId;

            req.Headers.Add("User-Agent", userAgent);
            req.Headers.Add("x-ms-user-agent", userAgent);
            req.Headers.Add("x-ms-client-environment-id", $"/providers/Microsoft.PowerApps/environments/{this._environmentId}");
            req.Headers.Add("x-ms-client-request-id", clientRequestId);
            req.Headers.Add("x-ms-correlation-id", correlationId);

            if (!string.IsNullOrWhiteSpace(diagnosticOptions?.ClientSessionId))
            {
                req.Headers.Add("x-ms-client-session-id", diagnosticOptions.ClientSessionId);
            }

            if (!string.IsNullOrWhiteSpace(diagnosticOptions?.ClientTenantId))
            {
                req.Headers.Add("x-ms-client-tenant-id", diagnosticOptions.ClientTenantId);
            }

            if (!string.IsNullOrWhiteSpace(diagnosticOptions?.ClientObjectId))
            {
                req.Headers.Add("x-ms-client-object-id", diagnosticOptions.ClientObjectId);
            }
        }

        private static Uri GetBaseUrlFromOpenApiDocument(OpenApiDocument document)
        {
            ConnectorErrors errors = new ConnectorErrors();
            Uri uri = document.GetFirstServerUri(errors);

            if (uri is null)
            {
                errors.AddError("Swagger document doesn't contain an endpoint");
            }

            if (errors.HasErrors)
            {
                throw new PowerFxConnectorException(string.Join(", ", errors.Errors));
            }

            return uri;
        }
    }
}
