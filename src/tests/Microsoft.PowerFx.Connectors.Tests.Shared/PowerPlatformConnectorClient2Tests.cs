// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class PowerPlatformConnectorClient2Tests
    {
        private static Task<string> DummyTokenProvider(CancellationToken ct) => Task.FromResult("dummy-token");

        private class DummyHandler : HttpMessageHandler
        {
            public HttpRequestMessage LastRequest { get; private set; }
            
            public Func<HttpRequestMessage, HttpResponseMessage> OnSend { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
#pragma warning disable CA2000 // Dispose objects before losing scope
                var response = OnSend?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK);
#pragma warning restore CA2000 // Dispose objects before losing scope
                return Task.FromResult(response);
            }
        }

        [Fact]
        public void Constructor_ThrowsOnNullArguments()
        {
            using var handler = new DummyHandler();
            Assert.Throws<ArgumentNullException>(() => PowerPlatformConnectorHelper.FromUri((Uri)null, "env", "conn", DummyTokenProvider, handler));
            Assert.Throws<ArgumentNullException>(() => PowerPlatformConnectorHelper.FromUri(new Uri("https://test"), null, "conn", DummyTokenProvider, handler));
            Assert.Throws<ArgumentNullException>(() => PowerPlatformConnectorHelper.FromUri(new Uri("https://test"), "env", null, DummyTokenProvider, handler));
            Assert.Throws<ArgumentNullException>(() => PowerPlatformConnectorHelper.FromUri(new Uri("https://test"), "env", "conn", null, handler));
        }

        [Fact]
        public void Constructor_ThrowsOnRelativeOrHttpUri()
        {
            using var handler = new DummyHandler();
            Assert.Throws<PowerFxConnectorException>(() => PowerPlatformConnectorHelper.FromUri(new Uri("/relative", UriKind.Relative), "env", "conn", DummyTokenProvider, handler));
            Assert.Throws<PowerFxConnectorException>(() => PowerPlatformConnectorHelper.FromUri(new Uri("http://test"), "env", "conn", DummyTokenProvider, handler));
        }

        [Fact]
        public async Task NormalizeUrl_AddsSchemeIfMissing()
        {
            using var handler = new DummyHandler();
            using var loggingHandler = new LoggingHandler(handler);
            var (client, baseUri) = PowerPlatformConnectorHelper.FromBaseUrl("test.com", "env", "conn", DummyTokenProvider, loggingHandler);
            using var httpInvoker = new HttpMessageInvoker(client);
            using var dummyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            _ = await httpInvoker.SendAsync(dummyRequest, CancellationToken.None);

            Assert.Contains(loggingHandler.Logs, log => log.Contains("Request: GET https://test.com/api/test"));
            Assert.NotNull(client);
        }

        [Fact]
        public async Task SendAsync_SetsHeadersAndToken()
        {
            using var handler = new DummyHandler
            {
                OnSend = req =>
                {
                    Assert.Equal("Bearer", req.Headers.Authorization.Scheme);
                    Assert.Equal("dummy-token", req.Headers.Authorization.Parameter);
                    Assert.Contains("User-Agent", req.Headers.ToString());
                    Assert.Contains("x-ms-user-agent", req.Headers.ToString());
                    Assert.Contains("x-ms-client-environment-id", req.Headers.ToString());
                    Assert.Contains("x-ms-client-request-id", req.Headers.ToString());
                    Assert.Contains("x-ms-correlation-id", req.Headers.ToString());
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            };

            var (client, baseUri) = PowerPlatformConnectorHelper.FromBaseUrl("https://test.com", "env", "conn", DummyTokenProvider, handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            using var httpInvoker = new HttpMessageInvoker(client);
            var response = await httpInvoker.SendAsync(request, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ThrowsOnPathTraversal()
        {
            using var handler = new DummyHandler();
            var (client, baseUri) = PowerPlatformConnectorHelper.FromBaseUrl("https://test.com/path1/", "env", "conn", DummyTokenProvider, handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/../malicious");
            using var httpClient = new HttpClient(client) { BaseAddress = baseUri };
            await Assert.ThrowsAsync<ArgumentException>(() => httpClient.SendAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task SendAsync_DoesNotChangePathQueryFragmentMethodOrContent()
        {
            using var handler = new DummyHandler
            {
                OnSend = req =>
                {
                    Assert.Equal(HttpMethod.Post, req.Method);
                    Assert.Equal("https://test.com/api/test/path?x=1&y=2#section", req.RequestUri.OriginalString);

                    // Content check
                    var contentTask = req.Content.ReadAsStringAsync();
                    contentTask.Wait();
                    Assert.Equal("{\"sample\":\"data\"}", contentTask.Result);

                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            };

            var (client, baseUri) = PowerPlatformConnectorHelper.FromBaseUrl(
                "https://test.com",
                "env",
                "conn",
                DummyTokenProvider,
                handler);

            var uriWithEverything = new Uri("/api/test/path?x=1&y=2#section", UriKind.Relative);
            using var request = new HttpRequestMessage(HttpMethod.Post, uriWithEverything)
            {
                Content = new StringContent("{\"sample\":\"data\"}", System.Text.Encoding.UTF8, "application/json")
            };

            using var httpInvoker = new HttpMessageInvoker(client);
            var response = await httpInvoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_DoesNotChangePathQueryFragmentOrMethod_ForGetRequest()
        {
            using var handler = new DummyHandler
            {
                OnSend = req =>
                {
                    // Ensure the method is GET
                    Assert.Equal(HttpMethod.Get, req.Method);

                    // Ensure path, query, and fragment are preserved
                    Assert.Equal("https://example.com/api/test/get?param1=value1&param2=value2#top", req.RequestUri.OriginalString);

                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            };

            var (client, baseUri) = PowerPlatformConnectorHelper.FromBaseUrl(
                "https://example.com",
                "env",
                "conn",
                DummyTokenProvider,
                handler);

            var uri = new Uri("/api/test/get?param1=value1&param2=value2#top", UriKind.Relative);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            using var httpClient = new HttpClient(client) { BaseAddress = baseUri };
            var response = await httpClient.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PowerPlatformConnectorClient2Logging()
        {
            // dummy handler returns Accepted status
            using var dummyHandler = new DummyHandler
            {
                OnSend = _ => new HttpResponseMessage(HttpStatusCode.Accepted)
            };

            using var loggingHandler = new LoggingHandler(dummyHandler);

            var (client, baseUri) = PowerPlatformConnectorHelper.FromUri(
                new Uri("https://test.com"),
                "env",
                "conn",
                DummyTokenProvider,
                loggingHandler);
            using var invoker = new HttpMessageInvoker(client);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");

            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Collection(
                loggingHandler.Logs, 
                log => Assert.Contains("Request: GET https://test.com/api/test", log),
                log => Assert.Contains("Response: Accepted", log));
        }

        // Demonstrates how to chain a logging handler with the PowerPlatformConnectorClient2
        private class LoggingHandler : DelegatingHandler
        {
            public IList<string> Logs { get; } = new List<string>();

            public LoggingHandler(HttpMessageHandler innerHandler) 
                : base(innerHandler) 
            { 
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Logs.Add($"Request: {request.Method} {request.RequestUri}");
                var response = await base.SendAsync(request, cancellationToken);
                Logs.Add($"Response: {response.StatusCode}");
                return response;
            }
        }
    }
}
