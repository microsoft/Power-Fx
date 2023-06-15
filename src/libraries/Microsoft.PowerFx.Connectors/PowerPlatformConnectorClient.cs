﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Http handler to invoke Power Platform connectors. 
    /// This accepts HttpRequestMessages described the swagger, transforms the request, and forwards them to Connector endpoints.
    /// </summary>
    public class PowerPlatformConnectorClient : HttpClient
    {
        /// <summary>
        /// For telemetry - assembly version stamp. 
        /// </summary>
        public static string Version => typeof(PowerPlatformConnectorClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];

        /// <summary>
        /// Session Id for telemetry.
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString(); // "f4d37a97-f1c7-4c8c-80a6-f300c651568d"

        private readonly HttpMessageInvoker _client;

        public string ConnectionId { get; }

        // For example, "firstrelease-001.azure-apim.net" 
        public string Endpoint => BaseAddress.Authority;

        /// <summary>
        /// Callback to get the auth token.
        /// Invoke as a callback since token may need to be refreshed. 
        /// </summary>
        public Func<Task<string>> GetAuthToken { get; }

        public string EnvironmentId { get; set; }

        public PowerPlatformConnectorClient(OpenApiDocument swaggerFile, string environmentId, string connectionId, Func<string> getAuthToken, HttpMessageInvoker httpInvoker = null)
            : this(swaggerFile.GetAuthority() ?? throw new ArgumentException("Swagger document doesn't contain an endpoint"), environmentId, connectionId, getAuthToken, httpInvoker)
        {
        }

        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<string> getAuthToken, HttpMessageInvoker httpInvoker = null)
            : this(endpoint, environmentId, connectionId, async () => getAuthToken(), httpInvoker)
        {
        }

        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<Task<string>> getAuthToken, HttpMessageInvoker httpInvoker = null)
        {
            _client = httpInvoker ?? new HttpClient();

            GetAuthToken = getAuthToken ?? throw new ArgumentNullException(nameof(getAuthToken));
            ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
            EnvironmentId = environmentId ?? throw new ArgumentNullException(nameof(environmentId));

            // Case insensitive comparison per RFC 9110 [4.2.3 http(s) Normalization and Comparison]
            if (endpoint.StartsWith($"{Uri.UriSchemeHttp}://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Cannot accept unsecure endpoint");
            }

            // Must set to allow callers to invoke SendAsync() via other helper methods.
            if (endpoint.StartsWith($"{Uri.UriSchemeHttps}://", StringComparison.OrdinalIgnoreCase))
            {
                BaseAddress = new Uri(endpoint);
            }
            else
            {
                BaseAddress = new Uri("https://" + endpoint); // Uri.Parse will validate endpoint syntax. 
            }
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using HttpRequestMessage req = await Transform(request).ConfigureAwait(false);
            return await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpRequestMessage> Transform(HttpRequestMessage request)
        {
            var url = request.RequestUri.ToString();
            if (request.RequestUri.IsAbsoluteUri)
            {
                // Client has Basepath set. 
                // x-ms-request-url needs relative URL. 
                throw new InvalidOperationException($"URL should be relative for x-ms-request-url property");
            }

            url = url.Replace("{connectionId}", ConnectionId);

            var method = request.Method;
            var authToken = await GetAuthToken().ConfigureAwait(false);

            var req = new HttpRequestMessage(HttpMethod.Post, $"https://{Endpoint}/invoke");
            req.Headers.Add("authority", Endpoint);
            req.Headers.Add("scheme", "https");
            req.Headers.Add("path", "/invoke");
            req.Headers.Add("x-ms-client-session-id", SessionId);
            req.Headers.Add("x-ms-request-method", method.ToString());
            req.Headers.Add("authorization", "Bearer " + authToken);
            req.Headers.Add("x-ms-client-environment-id", "/providers/Microsoft.PowerApps/environments/" + EnvironmentId);
            req.Headers.Add("x-ms-user-agent", $"PowerFx/{Version}");
            req.Headers.Add("x-ms-request-url", url);

            foreach (var header in request.Headers)
            {
                req.Headers.Add(header.Key, header.Value);
            }

            req.Content = request.Content;

            return req;
        }
    }
}
