// Copyright (c) Microsoft Corporation.
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

        private static PowerPlatformConnectorClient Client(HttpMessageInvoker httpMessageInvoker) => new (TestEndpoint, TestEnvironmentId, TestConnectionId, () => TestAuthToken, httpMessageInvoker);

        private static TestHandler TestHandler => new ();

        [Fact]
        public void PowerPlatformConnectorClient_Constructor()
        {
            using var invoker = new HttpMessageInvoker(TestHandler);
            using var client = Client(invoker);

            Assert.NotNull(client);
            Assert.Equal(TestEndpoint, client.Endpoint);
            Assert.Equal(TestEnvironmentId, client.EnvironmentId);
            Assert.Equal(TestConnectionId, client.ConnectionId);
            Assert.Equal(TestAuthToken, client.GetAuthToken());
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
        public void PowerPlatformConnectorClient_TransformRequest(string method, string extraHeaders = null, string content = null)
        {
            using var invoker = new HttpMessageInvoker(TestHandler);
            using var client = Client(invoker);
            using var request = new HttpRequestMessage(new HttpMethod(method), "/{connectionId}/test/someUri");

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

            var transformedRequest = client.Transform(request);

            Assert.NotNull(transformedRequest);
            Assert.Equal(new Uri("https://" + TestEndpoint + "/invoke"), transformedRequest.RequestUri);
            Assert.Equal(request.Content, transformedRequest.Content);

            ValidateHeaders(request, transformedRequest);
            Assert.Null(TestHandler.Request);
        }

        private static void ValidateHeaders(HttpRequestMessage request, HttpRequestMessage transformedRequest)
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
                        Assert.Equal(request.Method.ToString(), header.Value.First());
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
