// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Web;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public record InitParams(Features features = null, ParserOptions options = null, IPowerFxScopeFactory scopeFactory = null);

    public record LSPError(JsonRpcHelper.ErrorCode code, string message = null);

    public partial class LanguageServerTestBase : PowerFxTest
    {
        private LanguageServerForTesting TestServer { get; set; }

        private TestHandlerFactory HandlerFactory { get; set; }

        public TestLogger Logger { get; set; }

        public TestHostTaskExecutor HostTaskExecutor { get; set; }

        public LanguageServerTestBase()
            : base()
        {
            Init();
        }

        internal void Init(InitParams initParams)
        {
            var config = new PowerFxConfig(features: initParams?.features ?? Features.None);
            config.AddFunction(new BehaviorFunction());
            config.AddFunction(new AISummarizeFunction());

            var engine = new Engine(config);
            HandlerFactory = new TestHandlerFactory();
            HostTaskExecutor = new TestHostTaskExecutor();
            var random = new Random();
            var useHostTaskExecutor = random.Next(0, 2) == 1;

            var scopeFactory = initParams?.scopeFactory ?? new TestPowerFxScopeFactory(
                               (string documentUri) => engine.CreateEditorScope(initParams?.options, GetFromUri(documentUri)));

            TestServer = new LanguageServerForTesting(scopeFactory, HandlerFactory, useHostTaskExecutor ? HostTaskExecutor : null, Logger);
        }

        internal void Init()
        {
            Init(new InitParams());
        }

        // The convention for getting the context from the documentUri is arbitrary and determined by the host. 
        internal static ReadOnlySymbolTable GetFromUri(string documentUri)
        {
            var uriObj = new Uri(documentUri);
            var json = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            json ??= "{}";

            var record = (RecordValue)FormulaValueJSON.FromJson(json);
            return ReadOnlySymbolTable.NewFromRecord(record.Type);
        }

        internal static LSPError AssertErrorPayload(string response, string id, JsonRpcHelper.ErrorCode expectedCode)
        {
            Assert.NotNull(response);
            var deserializedResponse = JsonDocument.Parse(response);
            var root = deserializedResponse.RootElement;
            Assert.True(root.TryGetProperty("id", out var responseId));
            Assert.Equal(id, responseId.GetString());
            Assert.True(root.TryGetProperty("error", out var errElement));
            Assert.True(errElement.TryGetProperty("code", out var codeElement));
            var code = (JsonRpcHelper.ErrorCode)codeElement.GetInt32();
            Assert.Equal(expectedCode, code);
            Assert.True(root.TryGetProperty("fxVersion", out var fxVersionElement));
            Assert.Equal(Engine.AssemblyVersion, fxVersionElement.GetString());
            string message = null;
            if (errElement.TryGetProperty("message", out var messageElement))
            {
                message = messageElement.GetString();
            }

            return new LSPError(code, message);
        }

        internal static string GetOutputAtIndexInSerializedResponse(string response, string id = null,  string method = null, int index = -1)
        {
            Assert.NotNull(response);
            try
            {
                var possiblyArray = JsonSerializer.Deserialize<List<string>>(response, LanguageServerHelper.DefaultJsonSerializerOptions);
                if (possiblyArray == null || possiblyArray.Count == 0)
                {
                    return response;
                }

                if (index >= 0 && index < possiblyArray.Count)
                {
                    response = possiblyArray[index];
                }
                else if (method != null) 
                {
                    var match = possiblyArray.Where(item => item.Contains(method)).FirstOrDefault();
                    if (match != null || match != default)
                    {
                        response = match;
                    }
                }
                else
                {
                    var match = possiblyArray.Where(item => item.Contains(id)).FirstOrDefault();
                    if (match != null || match != default)
                    {
                        response = match;
                    }
                }
            }
            catch (JsonException)
            {
                // ignore
            }

            return response;
        }

        internal static T AssertAndGetResponsePayload<T>(string response, string id, int index = -1)
        {
            response = GetOutputAtIndexInSerializedResponse(response, id, null, index);
            var deserializedResponse = JsonDocument.Parse(response);
            var root = deserializedResponse.RootElement;
            root.TryGetProperty("id", out var responseId);
            Assert.Equal(id, responseId.GetString());
            root.TryGetProperty("result", out var resultElement);
            Assert.True(root.TryGetProperty("fxVersion", out var fxVersionElement));
            Assert.Equal(Engine.AssemblyVersion, fxVersionElement.GetString());
            var paramsObj = JsonSerializer.Deserialize<T>(resultElement.GetRawText(), LanguageServerHelper.DefaultJsonSerializerOptions);
            return paramsObj;
        }

        internal static T AssertAndGetNotificationParams<T>(string response, string method, int index = -1)
        {
            Assert.NotNull(response);
            response = GetOutputAtIndexInSerializedResponse(response, null, method, index);
            var notification = GetOutputAtIndexInSerializedResponse(response);
            var deserializedNotification = JsonDocument.Parse(notification);
            var root = deserializedNotification.RootElement;
            Assert.True(root.TryGetProperty("method", out var methodElement));
            Assert.Equal(method, methodElement.GetString());
            Assert.True(root.TryGetProperty("params", out var paramsElement));
            T paramsObj;

            if (method == CustomProtocolNames.PublishExpressionType)
            {
                paramsObj = JsonRpcHelper.Deserialize<T>(paramsElement.GetRawText());
            }
            else
            {
                paramsObj = JsonSerializer.Deserialize<T>(paramsElement.GetRawText(), LanguageServerHelper.DefaultJsonSerializerOptions);
            }

            return paramsObj;
        }

        internal static string GetUri(string queryParams = null)
        {
            var uriBuilder = new UriBuilder("powerfx://app")
            {
                Query = queryParams ?? string.Empty
            };
            return uriBuilder.Uri.AbsoluteUri;
        }

        internal static (string payload, string id) GetRequestPayload<T>(T paramsObj, string method, string id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var payload = JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                method,
                @params = paramsObj
            }, LanguageServerHelper.DefaultJsonSerializerOptions);
            return (payload, id);
        }

        internal static string GetNotificationPayload<T>(T paramsObj, string method)
        {
            var payload = JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method,
                @params = paramsObj
            }, LanguageServerHelper.DefaultJsonSerializerOptions);
            return payload;
        }

        internal static TextDocumentIdentifier GetTextDocument(string uri = null)
        {
            return new TextDocumentIdentifier() { Uri = uri ?? GetUri() };
        }

        internal static string GetExpression(LanguageServerRequestBaseParams requestParams)
        {
            if (requestParams?.Text != null)
            {
                return requestParams.Text;
            }

            var uri = new Uri(requestParams.TextDocument.Uri);
            return HttpUtility.ParseQueryString(uri.Query).Get("expression");
        }

        internal static Position GetPosition(int offset, int line = 0)
        {
            return new Position()
            {
                Line = line,
                Character = offset
            };
        }

        internal static ParserOptions GetParserOptions(bool withAllowSideEffects)
        {
            return withAllowSideEffects ? new ParserOptions() { AllowsSideEffects = true } : null;
        }
    }
}
