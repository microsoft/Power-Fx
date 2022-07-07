// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        public static string Version { get; } = typeof(PowerPlatformConnectorClient).Assembly.GetName().Version.ToString();
        
        /// <summary>
        /// Session Id for telemetry.
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString(); // "f4d37a97-f1c7-4c8c-80a6-f300c651568d"

        private readonly HttpMessageInvoker _client;

        public string ConnectionId { get; } 

        // For example, "firstrelease-001.azure-apim.net" 
        public string Endpoint { get; }

        /// <summary>
        /// Callback to get the auth token. 
        /// Invoke as a callback since token may need to be refreshed. 
        /// </summary>
        public Func<string> GetAuthToken { get; }

        public string EnvironmentId { get; set; }

        public PowerPlatformConnectorClient(string endpoint, string environmentId, string connectionId, Func<string> getAuthToken, HttpMessageInvoker httpInvoker = null)
        {
            _client = httpInvoker ?? new HttpClient();

            GetAuthToken = getAuthToken ?? throw new ArgumentNullException(nameof(getAuthToken));
            ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
            EnvironmentId = environmentId ?? throw new ArgumentNullException(nameof(environmentId));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            // Must set to allow callers to invoke SendAsync() via other helper methods.
            BaseAddress = new Uri("https://" + endpoint); // Uri.Parse will validate endpoint syntax. 
        }
                
        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpRequestMessage req = Transform(request);

            var response = await _client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            return response;
        }

        public HttpRequestMessage Transform(HttpRequestMessage request)
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
            var authToken = GetAuthToken();

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
