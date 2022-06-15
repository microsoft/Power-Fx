// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Simulate a test server (Connector, ASP.Net site, etc). 
    // This logs all received SendAsync() calls to _log for easy verification. 
    // Test can call SetResponse() to set what each SendAsync() should return.
    internal class LoggingTestServer : HttpMessageHandler
    {
        // Log HTTP calls. 
        public StringBuilder _log = new StringBuilder();

        public OpenApiDocument _apiDocument;

        public LoggingTestServer(string swaggerName)
        {
            _apiDocument = Helpers.ReadSwagger(swaggerName);
        }

        // Set the response, returned by SendAsync
        public HttpResponseMessage _nextResponse;

        public void SetResponseFromFile(string filename)
        {
            var json = Helpers.ReadAllText(filename);
            SetResponse(json);
        }

        public void SetResponse(string json)
        {
            Assert.Null(_nextResponse);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            _nextResponse = response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.Method;
            var url = request.RequestUri.ToString();

            _log.AppendLine($"{method} {url}");

            foreach (var kv in request.Headers.OrderBy(x => x.Key))
            {
                var headerName = kv.Key;
                var value = kv.Value.First();
                _log.AppendLine($" {headerName}: {value}");
            }

            var response = _nextResponse;
            _nextResponse = null;
            return response;
        }
    }
}
