// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Simulate a test server (Connector, ASP.Net site, etc). 
    // This logs all received SendAsync() calls to _log for easy verification. 
    // Test can call SetResponse() to set what each SendAsync() should return.
    internal class LoggingTestServer : HttpMessageHandler
    {
        // Log HTTP calls. 
        public StringBuilder _log = new ();

        public OpenApiDocument _apiDocument;

        public LoggingTestServer(string swaggerName)
        {
            _apiDocument = Helpers.ReadSwagger(swaggerName);
        }

        // Set the response, returned by SendAsync
        public HttpResponseMessage _nextResponse;

        public void SetResponseFromFile(string filename, HttpStatusCode status = HttpStatusCode.OK)
        {
            var text = Helpers.ReadAllText(filename);
            SetResponse(text, status);
        }

        public void SetResponse(string text, HttpStatusCode status = HttpStatusCode.OK)
        {
            Assert.Null(_nextResponse);

            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(text, Encoding.UTF8, OpenApiExtensions.ContentType_ApplicationJson)
            };
            _nextResponse = response;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _nextResponse?.Dispose();
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

            var httpContent = request?.Content;
            if (httpContent != null)
            {
                if (httpContent.Headers != null)
                {
                    foreach (var h in httpContent.Headers)
                    {
                        _log.AppendLine($" [content-header] {h.Key}: {string.Join(", ", h.Value)}");
                    }
                }

                var content = await httpContent.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    _log.AppendLine($" [body] {content}");
                }
            }

            var response = _nextResponse;
            response.RequestMessage = request;
            _nextResponse = null;
            return response;
        }
    }
}
