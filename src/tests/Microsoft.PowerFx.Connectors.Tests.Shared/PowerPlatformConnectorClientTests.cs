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
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/microsoft/Power-Fx/issues/2940
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
            Assert.Equal(TestAuthToken, await client.GetAuthToken());
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
                foreach (var kvp in extraHeaders.Split(';'))
                {
                    var hv = kvp.Split(':');
                    request.Headers.Add(hv.First(), hv.Skip(1));
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content);
            }

            var transformedRequest = await client.Transform(request);

            Assert.NotNull(transformedRequest);
            Assert.Equal(new Uri("https://" + TestEndpoint + "/invoke"), transformedRequest.RequestUri);
            Assert.Equal(request.Content, transformedRequest.Content);

            ValidateHeaders(request, transformedRequest);
            Assert.Null(TestHandler.Request);
        }

        // x-ms-request-url is resolved by the connector gateway. A network-path reference names an authority
        // rather than a path; validate our client side short circuit
        [Theory]
        [InlineData("//evil.contoso.com/steal")]
        [InlineData("//evil.contoso.com")]
        [InlineData("/\\evil.contoso.com/steal")]
        [InlineData("\\/evil.contoso.com/steal")]
        [InlineData("\\\\evil.contoso.com/steal")]
        public async Task PowerPlatformConnectorClient_TransformRejectsNetworkPathReference(string relativeUrl)
        {
            var client = Client;
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(relativeUrl, UriKind.Relative));

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.Transform(request));
        }

        [Theory]
        [InlineData("/apim/cognitiveservicestextanalytics/16e7c181/language/:analyze-conversations?api-version=2022-05-01")]
        [InlineData("/apim/sharepointonline/6fb0a1a8/datasets/https%253A%252F%252Fcontoso.sharepoint.com%252Fsites%252FSite17/alltables")]
        [InlineData("/apim/sql/5f57ec83/v2/datasets/contoso-sql.database.windows.net,connectortest/procedures")]
        [InlineData("/apim/sql/5f57ec83/{queryPart}")]
        [InlineData("/apim/sql/c1a4e9f5/tables/%5Bdbo%5D.%5BCustomers%5D/items?api-version=2015-09-01&$top=101")]

        // "." is a no-op segment and ".." inside a segment is not a dot-segment; neither escapes the path.
        [InlineData("/apim/sql/5f57ec83/./tables")]
        [InlineData("/apim/sharepointonline/6fb0a1a8/files/report..pdf")]
        [InlineData("/apim/sharepointonline/6fb0a1a8/files/%252e%252e/report.pdf")]

        // Dot-segments in the query string are never resolved as path segments.
        [InlineData("/apim/sql/5f57ec83/tables?path=../parent")]
        public async Task PowerPlatformConnectorClient_TransformPreservesSafeUrl(string relativeUrl)
        {
            var client = Client;
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(relativeUrl, UriKind.Relative));

            using var transformedRequest = await client.Transform(request);

            Assert.Equal(new Uri("https://" + TestEndpoint + "/invoke"), transformedRequest.RequestUri);
            Assert.Equal(relativeUrl, transformedRequest.Headers.GetValues("x-ms-request-url").Single());
        }

        // BaseAddress is settable on HttpClient, so the /invoke target must stay HTTPS even if it is reassigned.
        [Fact]
        public async Task PowerPlatformConnectorClient_TransformDoesNotDowngradeScheme()
        {
            var client = Client;
            client.BaseAddress = new Uri("http://localhost:1234");

            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/{TestConnectionId}/test/someUri", UriKind.Relative));
            using var transformedRequest = await client.Transform(request);

            Assert.Equal(new Uri("https://localhost:1234/invoke"), transformedRequest.RequestUri);
        }

        // The gateway resolves x-ms-request-url relative to the connector path, so the value must be a
        // plain relative path. PowerPlatformConnectorClient2 gets this from Uri.IsBaseOf.
        [Theory]
        [InlineData("/../malicious")]
        [InlineData("/apim/sql/conn/../../../evil")]
        [InlineData("/apim/sql/conn/..")]
        [InlineData("/%2e%2e/malicious")]
        [InlineData("/%2E%2E/malicious")]
        [InlineData("/..%2fmalicious")]
        [InlineData("/..%5Cmalicious")]
        [InlineData("/apim/sql/conn/..%2F..%2Fevil")]
        [InlineData("\\..\\malicious")]
        public async Task PowerPlatformConnectorClient_TransformRejectsPathTraversal(string relativeUrl)
        {
            var client = Client;
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(relativeUrl, UriKind.Relative));

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.Transform(request));
        }

        // The client sets these headers itself, so a value on the incoming request must not be
        // appended alongside it.
        [Theory]
        [InlineData("x-ms-request-url", "/evil/path")]
        [InlineData("X-MS-REQUEST-URL", "/evil/path")]
        [InlineData("x-ms-request-method", "DELETE")]
        [InlineData("x-ms-client-environment-id", "/providers/Microsoft.PowerApps/environments/EVIL")]
        [InlineData("x-ms-client-session-id", "00000000-0000-0000-0000-000000000000")]
        [InlineData("x-ms-user-agent", "EvilAgent/1.0")]
        [InlineData("x-ms-enable-selects", "false")]
        [InlineData("Authorization", "Bearer EvilToken")]
        [InlineData("authority", "evil.contoso.com")]
        [InlineData("scheme", "http")]
        [InlineData("path", "/evil")]
        public async Task PowerPlatformConnectorClient_TransformDropsReservedHeaders(string headerName, string headerValue)
        {
            var client = Client;
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/{TestConnectionId}/test/someUri", UriKind.Relative));
            request.Headers.TryAddWithoutValidation(headerName, headerValue);

            using var transformedRequest = await client.Transform(request);

            Assert.DoesNotContain(headerValue, transformedRequest.Headers.GetValues(headerName));
            Assert.Single(transformedRequest.Headers.GetValues(headerName));
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
                    case "x-ms-enable-selects":
                        Assert.Equal("true", header.Value.First());
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

            Assert.Equal(request.Headers.Count() + 10, transformedRequest.Headers.Count());
        }
    }

    internal class TestHandler : DelegatingHandler
    {
        internal HttpRequestMessage Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Request = request;
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
