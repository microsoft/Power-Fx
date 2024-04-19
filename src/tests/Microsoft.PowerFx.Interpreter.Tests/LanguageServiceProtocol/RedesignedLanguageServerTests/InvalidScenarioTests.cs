// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class ErrorThrowingHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => false;

        public string LspMethod => TextDocumentNames.DidOpen;

        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            throw new Exception("Test Exception");
        }
    }

    public partial class LanguageServerTestBase
    {
        [Fact]
        public async Task TestTopParseError()
        {
            var rawResponse = await TestServer.OnDataReceivedAsync("parse error").ConfigureAwait(false);
            AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.ParseError);
        }

        // Exceptions can be thrown oob, test we can register a hook and receive.
        // Check for exceptions if the scope object we call back to throws
        [Fact]
        public async Task TestLogCallbackExceptions()
        {
            // Arrange
            HandlerFactory.SetHandler(TextDocumentNames.DidOpen, new ErrorThrowingHandler());

            // Act
            var payload = GetDidOpenPayload(
            new DidOpenTextDocumentParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = "https://none",
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "123"
                }
            });
            var rawResponse = await TestServer.OnDataReceivedAsync(payload).ConfigureAwait(false);

            // Assert
            var error = AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.InternalError);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(TestServer.UnhandledExceptions[0].GetDetailedExceptionMessage(), error.message);
        }

        [Fact]
        public async Task TestLanguageServerCommunication()
        {
            // bad payload
            var response1 = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new { })).ConfigureAwait(false);

            // bad jsonrpc payload
            var response2 = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0"
            })).ConfigureAwait(false);

            // bad notification payload
            var response3 = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "unknown",
                @params = "unkown"
            })).ConfigureAwait(false);

            // bad request payload
            var response4 = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "abc",
                method = "unknown",
                @params = "unkown"
            })).ConfigureAwait(false);

            // verify we have 4 json rpc responeses
            AssertErrorPayload(response1, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.InvalidRequest);
            AssertErrorPayload(response2, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.InvalidRequest);
            AssertErrorPayload(response3, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.MethodNotFound);
            AssertErrorPayload(response4, "abc", LanguageServerProtocol.JsonRpcHelper.ErrorCode.MethodNotFound);
        }

        [Fact]
        public async Task TestHandlerIsMissing()
        {
            // Arrange
            HandlerFactory.SetHandler(TextDocumentNames.DidOpen, null);

            // Act
            var payload = GetDidOpenPayload(
            new DidOpenTextDocumentParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = "https://none",
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "123"
                }
            });
            var rawResponse = await TestServer.OnDataReceivedAsync(payload).ConfigureAwait(false);

            // Assert
            AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.MethodNotFound);
        }
    }
}
