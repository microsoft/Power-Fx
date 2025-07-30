// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using static Microsoft.PowerFx.Connectors.ConnectorSettings;
using static Microsoft.PowerFx.Connectors.ConnectorType;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Delegation handler for invoking operations of Power Platform connectors.
    /// </summary>
    internal class PowerPlatformConnectorClient2 : DelegatingHandler
    {
        private readonly BearerAuthTokenProvider _tokenProvider;
        private readonly string _environmentId;
        private readonly string _sessionId;

        private readonly Uri _baseUri;

        internal Uri BaseUri => _baseUri;

        public string ConnectionId { get; }

        public string UserAgent { get; }

        public string EnvironmentId => _environmentId;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient2"/> class.
        /// </summary>
        /// <param name="document">Document used for extracting the base URL.</param>
        /// <param name="httpMessageHandler">HTTP message invoker.</param>
        /// <param name="tokenProvider">Bearer token provider.</param>
        /// <param name="environmentId">Environment ID.</param>
        /// <param name="connectionId">Connection ID.</param>
        /// <param name="userAgent">User agent.</param>
        /// <param name="sessionId"></param>
        public PowerPlatformConnectorClient2(
            OpenApiDocument document,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent,
            string sessionId)
            : this(GetBaseUrlFromOpenApiDocument(document), environmentId, connectionId, tokenProvider, httpMessageHandler, userAgent, sessionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient2"/> class using a string base URL.
        /// </summary>
        /// <param name="baseUrl">Base URL for requests (as string).</param>
        /// <param name="environmentId">Environment ID.</param>
        /// <param name="connectionId">Connection ID.</param>
        /// <param name="userAgent">User agent.</param>
        /// <param name="sessionId"></param>
        /// <param name="tokenProvider">Bearer token provider.</param>
        /// <param name="httpMessageHandler">HTTP message handler.</param>
        public PowerPlatformConnectorClient2(
            string baseUrl,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent,
            string sessionId)
            : this(NormalizeUrl(baseUrl), environmentId, connectionId, tokenProvider, httpMessageHandler, userAgent, sessionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient2"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for requests.</param>
        /// <param name="httpMessageHandler">HTTP message invoker.</param>
        /// <param name="tokenProvider">Bearer token provider.</param>
        /// <param name="environmentId">Environment ID.</param>
        /// <param name="connectionId">Connection ID.</param>
        /// <param name="userAgent">User agent.</param>
        /// <param name="sessionId"></param>
        public PowerPlatformConnectorClient2(
            Uri baseUrl,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent = null,
            string sessionId = null)
            : base(httpMessageHandler)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            this._tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            this._environmentId = environmentId ?? throw new ArgumentNullException(nameof(environmentId));
            this.ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
            this.UserAgent = string.IsNullOrWhiteSpace(userAgent) ? $"PowerFx/{Engine.AssemblyVersion}" : $"{userAgent} PowerFx/{Engine.AssemblyVersion}";
            this._sessionId = sessionId ?? Guid.NewGuid().ToString(); // "f4d37a97-f1c7-4c8c-80a6-f300c651568d"

            if (!baseUrl.IsAbsoluteUri)
            {
                throw new PowerFxConnectorException("Cannot accept relative URI");
            }
            else if (baseUrl.Scheme == Uri.UriSchemeHttp)
            {
                throw new PowerFxConnectorException("Cannot accept unsecure endpoint");
            }

            this._baseUri = GetBaseUri(baseUrl ?? throw new ArgumentNullException(nameof(baseUrl)));

            static Uri GetBaseUri(Uri uri)
            {
                var str = uri.GetLeftPart(UriPartial.Path);
                return new Uri(str, UriKind.Absolute);
            }
        }

        private static Uri NormalizeUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            // Add scheme if missing
            if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "https://" + baseUrl;
            }

            return new Uri(baseUrl, UriKind.Absolute);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            var finalUri = BuildFinalUri(request.RequestUri);

            request.RequestUri = finalUri;

            var token = await this._tokenProvider(cancellationToken)
                                 .ConfigureAwait(false);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            this.AddDiagnosticHeaders(request);

            //Prevent path-traversal
            if (!_baseUri.IsBaseOf(finalUri))
            {
                throw new ArgumentException(
                    $"Path traversal detected: {request.RequestUri}",
                    nameof(request.RequestUri));
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private Uri BuildFinalUri(Uri original)
        {
            // Absolute? keep it if it's under base
            if (original.IsAbsoluteUri)
            {
                if (!_baseUri.IsBaseOf(original))
                {
                    throw new ArgumentException("The URI must be relative or under the base.", nameof(original));
                }

                var replaced = original.OriginalString.Replace("{connectionId}", ConnectionId);
                return new Uri(replaced, UriKind.Absolute);
            }

            // Relative path case (current behavior)
            var path = original.OriginalString.Replace("{connectionId}", ConnectionId);
            return new Uri(_baseUri, path);
        }

        private void AddDiagnosticHeaders(HttpRequestMessage req)
        {
            if (req is null) 
            { 
                throw new ArgumentNullException(nameof(req)); 
            }

            var headers = req.Headers;

            headers.AddIfMissing("User-Agent", UserAgent);
            headers.AddIfMissing("x-ms-user-agent", UserAgent);

            var envValue = $"/providers/Microsoft.PowerApps/environments/{_environmentId}";
            headers.AddIfMissing("x-ms-client-environment-id", envValue);

            var clientRequestId = Guid.NewGuid().ToString();

            headers.AddIfMissing("x-ms-client-session-id", _sessionId);
            headers.AddIfMissing("x-ms-client-request-id", clientRequestId);
            headers.AddIfMissing("x-ms-correlation-id", clientRequestId);
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
