﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class PowerPlatformConnectorClientTests : PowerFxTest
    {
        private const string TestEndpoint = "localhost:1234";
        private const string TestEnvironmentId = "2d8a766b-cbbf-4975-a8fe-550b2152795b";
        private const string TestConnectionId = "9f8196668cbd431990bcca95b3ec1e23";
        private const string TestAuthToken = "AuthToken1234";

        private TestHandler TestHandler => new ();

        private HttpMessageInvoker HttpMessageInvoker => new (TestHandler);

        private PowerPlatformConnectorClient Client => new (TestEndpoint, TestEnvironmentId, TestConnectionId, async () => TestAuthToken, HttpMessageInvoker);

        [Fact]
        public async Task PowerPlatformConnectorClient_Constructor()
        {
            var client = Client;

            Assert.NotNull(client);
            Assert.Equal(TestEndpoint, client.Endpoint);
            Assert.Equal(TestEnvironmentId, client.EnvironmentId);
            Assert.Equal(TestConnectionId, client.ConnectionId);
            Assert.Equal(TestAuthToken, await client.GetAuthToken().ConfigureAwait(false));
        }

        [Theory]
        [InlineData("Get")]
        [InlineData("Post")]
        [InlineData("Options")]
        [InlineData("Delete")]
        [InlineData("Head")]
        [InlineData("Patch")]
        [InlineData("Put")]
        [InlineData("Trace")]
        [InlineData("Get", "SomeHeader:SomeValue")]
        [InlineData("Get", "SomeHeader:SomeValue;SomeHeader2:SomeValue2")]
        [InlineData("Get", "SomeHeader:SomeValue;SomeHeader2:SomeValue2:AnotherValue")]
        [InlineData("Post", null, "abc")]
        [InlineData("Post", "SomeHeader:SomeValue", "abc")]
        public async Task PowerPlatformConnectorClient_TransformRequest(string method, string extraHeaders = null, string content = null)
        {
            var client = Client;
            using var request = new HttpRequestMessage(new HttpMethod(method), $"/{TestConnectionId}/test/someUri");

            if (!string.IsNullOrEmpty(extraHeaders))
            {
                foreach (var kvp in extraHeaders.Split(";"))
                {
                    var hv = kvp.Split(":");
                    request.Headers.Add(hv.First(), hv.Skip(1));
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content);
            }

            var transformedRequest = await client.Transform(request).ConfigureAwait(false);

            Assert.NotNull(transformedRequest);
            Assert.Equal(new Uri("https://" + TestEndpoint + "/invoke"), transformedRequest.RequestUri);
            Assert.Equal(request.Content, transformedRequest.Content);

            ValidateHeaders(request, transformedRequest);
            Assert.Null(TestHandler.Request);
        }

        private void ValidateHeaders(HttpRequestMessage request, HttpRequestMessage transformedRequest)
        {
            foreach (var header in transformedRequest.Headers)
            {
                switch (header.Key)
                {
                    case "authority":
                        Assert.Equal(TestEndpoint, header.Value.First());
                        break;
                    case "scheme":
                        Assert.Equal("https", header.Value.First());
                        break;
                    case "path":
                        Assert.Equal("/invoke", header.Value.First());
                        break;
                    case "x-ms-client-session-id":
                        Assert.True(Guid.TryParse(header.Value.First(), out _));
                        break;
                    case "x-ms-request-method":
                        Assert.Equal(request.Method.ToString().ToUpperInvariant(), header.Value.First());
                        break;
                    case "Authorization":
                        Assert.Equal($"Bearer {TestAuthToken}", header.Value.First());
                        break;
                    case "x-ms-client-environment-id":
                        Assert.Equal($"/providers/Microsoft.PowerApps/environments/{TestEnvironmentId}", header.Value.First());
                        break;
                    case "x-ms-user-agent":
                        Assert.StartsWith("PowerFx/", header.Value.First());
                        break;
                    case "x-ms-request-url":
                        Assert.Equal($"/{TestConnectionId}/test/someUri", header.Value.First());
                        break;
                    default:
                        Assert.True(request.Headers.Contains(header.Key), $"Missing {header.Key} header");
                        var reqHeaderValues = request.Headers.First(h => h.Key == header.Key).Value;
                        var transformedReqHeaderValues = request.Headers.First(h => h.Key == header.Key).Value;
                        Assert.Equal(reqHeaderValues.Count(), transformedReqHeaderValues.Count());
                        Assert.True(reqHeaderValues.All(rh => transformedReqHeaderValues.Contains(rh)));
                        break;
                }
            }

            Assert.Equal(request.Headers.Count() + 9, transformedRequest.Headers.Count());
        }
    }

    internal class TestHandler : DelegatingHandler
    {
        internal HttpRequestMessage Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Request = request;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
