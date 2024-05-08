// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        [Fact]
        public async Task TestNl2FxIsCanceledCorrectly()
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var nl2FxHandler = CreateAndConfigureNl2FxHandler();
            nl2FxHandler.Delay = true;
            nl2FxHandler.ThrowOnCancellation = true;
            nl2FxHandler.Nl2FxDelayTime = 2000;
            var payload = NL2FxMessageJson(documentUri);
            var source = HostCancelationHandler.Create(payload.id);
            source.CancelAfter(1200);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload, source.Token).ConfigureAwait(false);

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.RequestCancelled);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }

        [Fact]
        public async Task TestLanguageSeverHandlesNotificationToCancelNl2FxCorrectly()
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var nl2FxHandler = CreateAndConfigureNl2FxHandler();
            nl2FxHandler.Delay = true;
            nl2FxHandler.ThrowOnCancellation = true;
            nl2FxHandler.Nl2FxDelayTime = 2000;
            var payload = NL2FxMessageJson(documentUri);
            var source = HostCancelationHandler.Create(payload.id);
            var cancelRequestPayload = GetCancelRequestPayload(payload.id);

            // Act
            var nl2FxTask = TestServer.OnDataReceivedAsync(payload.payload, source.Token).ConfigureAwait(false);
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
                await TestServer.OnDataReceivedAsync(cancelRequestPayload, CancellationToken.None).ConfigureAwait(false);
            });
          
            var rawResponse = await nl2FxTask;

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.RequestCancelled);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }

        private static string GetCancelRequestPayload(string id)
        {
            return GetNotificationPayload(
            new CancelRequestParams
            {
                Id = id
            }, TextDocumentNames.CancelRequest);
        }
    }
}
