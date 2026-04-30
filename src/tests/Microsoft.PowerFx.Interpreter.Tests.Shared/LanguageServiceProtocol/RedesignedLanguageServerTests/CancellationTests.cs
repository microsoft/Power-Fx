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
            nl2FxHandler.ThrowOnCancellation = true;
            nl2FxHandler.Nl2FxDelayTime = 500;

            // Use a semaphore to synchronize cancellation: the token is cancelled only after
            // the NL2Fx handler has demonstrably entered NL2FxAsync, eliminating the
            // wall-clock race that made the original CancelAfter(500) approach flaky.
            using var handlerEntered = new SemaphoreSlim(0, 1);
            nl2FxHandler.NL2FxEnteredSemaphore = handlerEntered;

            var payload = NL2FxMessageJson(documentUri);
            using var source = new CancellationTokenSource();

            // Start the server call without awaiting — it will run concurrently.
            var responseTask = TestServer.OnDataReceivedAsync(payload.payload, source.Token);

            // Wait until the handler body has started, then cancel.
            await handlerEntered.WaitAsync();
            source.Cancel();

            // Act — now await the response with cancellation already signalled.
            var rawResponse = await responseTask;

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.RequestCancelled);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }
    }
}
