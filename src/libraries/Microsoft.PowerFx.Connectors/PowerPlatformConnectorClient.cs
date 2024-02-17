// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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

        public string UserAgent { get; }

        // For example, "firstrelease-001.azure-apim.net" 
        public string Endpoint => BaseAddress.Authority;

        /// <summary>
        /// Callback to get the auth token.
        /// Invoke as a callback since token may need to be refreshed.
        /// </summary>
        public Func<Task<string>> GetAuthToken { get; }

        public string EnvironmentId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="swaggerFile">Swagger file.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Function returning the JWT token.</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(OpenApiDocument swaggerFile, string environmentId, string connectionId, Func<string> getAuthToken, HttpMessageInvoker httpInvoker = null)
            : this(swaggerFile, environmentId, connectionId, getAuthToken, null, httpInvoker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="endpoint">APIM Endpoint.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Function returning the JWT token.</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<string> getAuthToken, HttpMessageInvoker httpInvoker = null)
            : this(endpoint, environmentId, connectionId, getAuthToken, null, httpInvoker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="endpoint">APIM Endpoint.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Async function returning the JWT token.</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<Task<string>> getAuthToken, HttpMessageInvoker httpInvoker = null)
            : this(endpoint, environmentId, connectionId, getAuthToken, null, httpInvoker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="swaggerFile">Swagger file.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Function returning the JWT token.</param>
        /// <param name="userAgent">Product UserAgent to add to Power-Fx one (Power-Fx/version).</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(OpenApiDocument swaggerFile, string environmentId, string connectionId, Func<string> getAuthToken, string userAgent, HttpMessageInvoker httpInvoker = null)
            : this(GetAuthority(swaggerFile), environmentId, connectionId, getAuthToken, userAgent, httpInvoker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="endpoint">APIM Endpoint.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Function returning the JWT token.</param>
        /// /// <param name="userAgent">Product UserAgent to add to Power-Fx one (Power-Fx/version).</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<string> getAuthToken, string userAgent, HttpMessageInvoker httpInvoker = null)
            : this(endpoint, environmentId, connectionId, async () => getAuthToken(), userAgent, httpInvoker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerPlatformConnectorClient"/> class.        
        /// </summary>
        /// <param name="endpoint">APIM Endpoint.</param>
        /// <param name="environmentId">Environment Id.</param>
        /// <param name="connectionId">Connection/connector Id.</param>
        /// <param name="getAuthToken">Async function returning the JWT token.</param>
        /// <param name="userAgent">Product UserAgent to add to Power-Fx one (Power-Fx/version).</param>
        /// <param name="httpInvoker">Optional HttpMessageInvoker. If not provided a default HttpClient is used.</param>
        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<Task<string>> getAuthToken, string userAgent, HttpMessageInvoker httpInvoker = null)
        {
            _client = httpInvoker ?? new HttpClient();

            GetAuthToken = getAuthToken ?? throw new ArgumentNullException(nameof(getAuthToken));
            ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
            EnvironmentId = environmentId ?? throw new ArgumentNullException(nameof(environmentId));
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? $"PowerFx/{Version}" : $"{userAgent} PowerFx/{Version}";

            // Case insensitive comparison per RFC 9110 [4.2.3 http(s) Normalization and Comparison]
            if (endpoint.StartsWith($"{Uri.UriSchemeHttp}://", StringComparison.OrdinalIgnoreCase))
            {
                throw new PowerFxConnectorException("Cannot accept unsecure endpoint");
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

        private static string GetAuthority(OpenApiDocument swaggerFile)
        {
            ConnectorErrors errors = new ConnectorErrors();
            string authority = swaggerFile.GetAuthority(errors);

            if (authority == null)
            {
                errors.AddError("Swagger document doesn't contain an endpoint");
            }

            if (errors.HasErrors)
            {
                throw new PowerFxConnectorException(string.Join(", ", errors.Errors));
            }

            return authority;
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using HttpRequestMessage req = await Transform(request).ConfigureAwait(false);
            return await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpRequestMessage> Transform(HttpRequestMessage request)
        {
            var url = request.RequestUri.OriginalString;
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
            req.Headers.Add("x-ms-request-method", method.ToString().ToUpperInvariant());
            req.Headers.Add("authorization", "Bearer " + authToken);
            req.Headers.Add("x-ms-client-environment-id", "/providers/Microsoft.PowerApps/environments/" + EnvironmentId);
            req.Headers.Add("x-ms-user-agent", UserAgent);
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
