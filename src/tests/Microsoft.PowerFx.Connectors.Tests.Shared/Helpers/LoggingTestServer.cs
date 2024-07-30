// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Xunit;
using Xunit.Abstractions;

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

        public bool SendAsyncCalled = false;

        public bool Live = false;

        public HttpClient LiveClient = null;

        public LoggingTestServer(string swaggerName, ITestOutputHelper output, bool live = false)
        {
            Live = live;
            _apiDocument = string.IsNullOrEmpty(swaggerName) ? null : Helpers.ReadSwagger(swaggerName, output);

            if (live)
            {
                LiveClient = new HttpClient();
            }
        }

        // Set the response, returned by SendAsync
#pragma warning disable CA2213 // Disposable fields should be disposed

        public HttpResponseMessage _nextResponse;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public object[] Responses = Array.Empty<object>(); // array of string or byte[]

        public HttpStatusCode[] Statuses = Array.Empty<HttpStatusCode>();

        public int CurrentResponse = 0;

        public bool ResponseSetMode = false;

        public void SetResponseSet(string filename)
        {
            if (!Live)
            {
                Responses = (Helpers.ReadStream(filename) as string).Split(new string[] { "~|~" }, StringSplitOptions.None).ToArray();
                Statuses = Enumerable.Repeat(HttpStatusCode.OK, Responses.Length).ToArray();
                CurrentResponse = 0;
                ResponseSetMode = true;
            }
        }

        public void SetResponseFromFiles(params string[] files)
        {
            if (files != null && files.Any() && !Live)
            {
                Responses = files.Select(file => Helpers.ReadStream(file)).ToArray();
                Statuses = Enumerable.Repeat(HttpStatusCode.OK, files.Length).ToArray();
                CurrentResponse = 0;
                ResponseSetMode = true;
            }
        }

        public void SetResponseFromFiles(params (string file, HttpStatusCode status)[] filesWithStatus)
        {
            if (filesWithStatus != null && filesWithStatus.Any() && !Live)
            {
                Responses = filesWithStatus.Select(fileWithStatus => GetFileText(fileWithStatus.file)).ToArray();
                Statuses = filesWithStatus.Select(fileWithStatus => fileWithStatus.status).ToArray();
                CurrentResponse = 0;
                ResponseSetMode = true;
            }
        }

        public void SetResponseFromFile(string filename, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (!Live)
            {
                var text = GetFileText(filename);
                SetResponse(text, status);
                ResponseSetMode = false;
            }
        }

        private static object GetFileText(string filename)
        {
            return !string.IsNullOrEmpty(filename) ? Helpers.ReadStream(filename) : string.Empty;
        }

        public void SetResponse(object data, HttpStatusCode status = HttpStatusCode.OK, string contentType = null)
        {
            if (!Live)
            {
                Assert.Null(_nextResponse);
                _nextResponse = GetResponseMessage(data, status, contentType);
            }
        }

        // We only support string & byte[] types (images)
        public HttpResponseMessage GetResponseMessage(object data, HttpStatusCode status, string contentType = null)
        {
            if (!Live && data is string str)
            {
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(str, Encoding.UTF8, contentType ?? OpenApiExtensions.ContentType_ApplicationJson)
                };
            }

            if (!Live && data is byte[] byteArray)
            {
                return new HttpResponseMessage(status)
                {
                    Content = new ByteArrayContent(byteArray)
                };
            }

            throw new NotImplementedException($"Unsupported data type or Live is {Live}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _nextResponse?.Dispose();
            LiveClient?.Dispose();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.Method;
            var url = request.RequestUri.ToString();

            SendAsyncCalled = true;
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

                var content = await httpContent.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrEmpty(content))
                {
                    _log.AppendLine($" [body] {content}");
                }
            }

            if (Live)
            {
                // Clone request as it can only be used once (https://stackoverflow.com/questions/25044166/how-to-clone-a-httprequestmessage-when-the-original-request-has-content)
                using HttpRequestMessage clone = new HttpRequestMessage(request.Method, request.RequestUri);

                // Copy the request's content (via a MemoryStream) into the cloned object
                using var ms = new MemoryStream();
                if (request.Content != null)
                {
                    await request.Content.CopyToAsync(ms, cancellationToken);

                    ms.Position = 0;
                    clone.Content = new StreamContent(ms);

                    // Copy the content headers
                    foreach (var h in request.Content.Headers)
                    {
                        clone.Content.Headers.Add(h.Key, h.Value);
                    }
                }

                clone.Version = request.Version;

#pragma warning disable CS0618 // Type or member is obsolete (HttpRequestMessage.Properties)
                foreach (KeyValuePair<string, object> prop in request.Properties)
                {
                    clone.Properties.Add(prop);
                }
#pragma warning restore CS0618 // Type or member is obsolete

                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                {
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return await LiveClient.SendAsync(clone, cancellationToken);
            }

            var response = ResponseSetMode ? GetResponseMessage(Responses[CurrentResponse], Statuses[CurrentResponse++]) : _nextResponse;
            if (response != null)
            {
                response.RequestMessage = request;
            }

            _nextResponse = null;
            return response ?? new HttpResponseMessage((HttpStatusCode)599);
        }
    }
}
