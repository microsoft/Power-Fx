// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers.Notifications;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class TestOnChangeHandler : OnDidChangeLanguageServerNotificationHandler
    {
        public int CallCounts = 0;

        public bool Delay { get; set; } = false;

        protected override async Task OnDidChange(LanguageServerOperationContext operationContext, DidChangeTextDocumentParams didChangeTextDocumentParams, CancellationToken cancellationToken)
        {
            if (this.Delay)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            CallCounts++;
        }

        public async Task OnDidChange(DidChangeTextDocumentParams didChangeTextDocumentParams, CancellationToken cancellationToken)
        {
            await OnDidChange(null, didChangeTextDocumentParams, cancellationToken).ConfigureAwait(false);
        }
    }

    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData("A+CountRows(B)", false, true)]
        [InlineData("Behavior(); A+CountRows(B)", true, true)]
        [InlineData("A+CountRows(B)", false, false)]
        [InlineData("Behavior(); A+CountRows(B)", true, false)]
        public async Task TestDidChange(string text, bool withAllowSideEffects, bool targetBackwardCompat)
        {
            Init(new InitParams(options: GetParserOptions(withAllowSideEffects)));
            var handler = CreateAndConfigureOnChangeHandler(targetBackwardCompat);
            handler.Delay = true;

            // test good formula
            var payload = GetNotificationPayload(
            new DidChangeTextDocumentParams()
            {
                TextDocument = new VersionedTextDocumentIdentifier()
                {
                    Uri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}",
                    Version = 1,
                },
                ContentChanges = new TextDocumentContentChangeEvent[]
                    {
                        new TextDocumentContentChangeEvent() { Text = text }
                    }
            }, TextDocumentNames.DidChange);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload).ConfigureAwait(false);
            var notification = GetDiagnosticsParams(rawResponse);
            Assert.Equal("powerfx://app?context={\"A\":1,\"B\":[1,2,3]}", notification.Uri);
            Assert.Empty(notification.Diagnostics);

            // test bad formula
            payload = GetNotificationPayload(
                new DidChangeTextDocumentParams()
                {
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Uri = "powerfx://app",
                        Version = 1,
                    },
                    ContentChanges = new TextDocumentContentChangeEvent[]
                    {
                        new TextDocumentContentChangeEvent() { Text = "AA" }
                    }
                },
                TextDocumentNames.DidChange);
            rawResponse = await TestServer.OnDataReceivedAsync(payload).ConfigureAwait(false);
            notification = GetDiagnosticsParams(rawResponse);
            Assert.Equal("powerfx://app", notification.Uri);
            Assert.Single(notification.Diagnostics);
            Assert.Equal("Name isn't valid. 'AA' isn't recognized.", notification.Diagnostics[0].Message);

            // some invalid cases
            rawResponse = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new { })).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.InvalidRequest);

            rawResponse = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didChange"
            })).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.InvalidRequest);

            rawResponse = await TestServer.OnDataReceivedAsync(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didChange",
                @params = string.Empty
            })).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, null, LanguageServerProtocol.JsonRpcHelper.ErrorCode.ParseError);

            Assert.True(handler.CallCounts == 2);
        }

        private static PublishDiagnosticsParams GetDiagnosticsParams(string response)
        {
            return AssertAndGetNotificationParams<PublishDiagnosticsParams>(response, TextDocumentNames.PublishDiagnostics);
        }

        private TestOnChangeHandler CreateAndConfigureOnChangeHandler(bool targetBackwardsCompat)
        {
            var handler = new TestOnChangeHandler();
            if (targetBackwardsCompat)
            {
                HandlerFactory.SetHandler(TextDocumentNames.DidChange, new BackwardsCompatibleOnDidChangeNotificationHandler((DidChangeTextDocumentParams parameters) =>
                {
                    handler.OnDidChange(parameters, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }));
            }
            else
            {
                HandlerFactory.SetHandler(TextDocumentNames.DidChange, handler);
            }

            return handler;
        }

        private void CheckBehaviorError(string response, bool expectBehaviorError, out Diagnostic[] diags)
        {
            diags = GetDiagnosticsParams(response).Diagnostics;

            if (expectBehaviorError)
            {
                Assert.Contains(diags, d => d.Message == StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag));
            }
            else
            {
                Assert.DoesNotContain(diags, d => d.Message == StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag));
            }
        }
    }
}
