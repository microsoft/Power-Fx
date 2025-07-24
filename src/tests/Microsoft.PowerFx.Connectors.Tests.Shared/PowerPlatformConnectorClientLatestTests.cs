// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
            Assert.Throws<ArgumentNullException>(() => new PowerPlatformConnectorClient2((Uri)null, "env", "conn", DummyTokenProvider, "ua", handler));
            Assert.Throws<ArgumentNullException>(() => new PowerPlatformConnectorClient2(new Uri("https://test"), null, "conn", DummyTokenProvider, "ua", handler));
            Assert.Throws<ArgumentNullException>(() => new PowerPlatformConnectorClient2(new Uri("https://test"), "env", null, DummyTokenProvider, "ua", handler));
            Assert.Throws<ArgumentNullException>(() => new PowerPlatformConnectorClient2(new Uri("https://test"), "env", "conn", null, "ua", handler));
        }

        [Fact]
        public void Constructor_ThrowsOnRelativeOrHttpUri()
        {
            using var handler = new DummyHandler();
            Assert.Throws<PowerFxConnectorException>(() => new PowerPlatformConnectorClient2(new Uri("/relative", UriKind.Relative), "env", "conn", DummyTokenProvider, "ua", handler));
            Assert.Throws<PowerFxConnectorException>(() => new PowerPlatformConnectorClient2(new Uri("http://test"), "env", "conn", DummyTokenProvider, "ua", handler));
        }

        [Fact]
        public void NormalizeUrl_AddsSchemeIfMissing()
        {
            using var handler = new DummyHandler();
            using var client = new PowerPlatformConnectorClient2("test.com", "env", "conn", DummyTokenProvider, "ua", handler);
            Assert.Equal("https://test.com", client.BaseUrlStr.OriginalString);
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

            using var client = new PowerPlatformConnectorClient2("https://test.com", "env", "conn", DummyTokenProvider, "ua", handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            using var httpInvoker = new HttpMessageInvoker(client);
            var response = await httpInvoker.SendAsync(request, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_ThrowsOnPathTraversal()
        {
            using var handler = new DummyHandler();
            using var client = new PowerPlatformConnectorClient2("https://test.com", "env", "conn", DummyTokenProvider, "ua", handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/../malicious");
            using var httpClient = new HttpClient(client);
            await Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.SendAsync(request, CancellationToken.None));
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

            using var client = new PowerPlatformConnectorClient2(
                "https://test.com",
                "env",
                "conn",
                DummyTokenProvider,
                "ua",
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

            using var client = new PowerPlatformConnectorClient2(
                "https://example.com",
                "env",
                "conn",
                DummyTokenProvider,
                "ua",
                handler);

            var uri = new Uri("/api/test/get?param1=value1&param2=value2#top", UriKind.Relative);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            using var httpClient = new HttpClient(client) { BaseAddress = client.BaseUrlStr };
            var response = await httpClient.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
