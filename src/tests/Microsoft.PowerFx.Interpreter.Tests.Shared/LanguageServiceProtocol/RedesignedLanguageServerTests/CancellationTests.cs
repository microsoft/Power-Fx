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
            nl2FxHandler.Nl2FxDelayTime = 800;
            var payload = NL2FxMessageJson(documentUri);
            using var source = new CancellationTokenSource();
            source.CancelAfter(500);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload, source.Token).ConfigureAwait(false);

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.RequestCancelled);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }
    }
}
