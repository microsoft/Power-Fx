// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class TestNL2FxHandler : NLHandler
    {
        public bool _supportsNL2Fx = true;
        public bool _supportsFx2NL = true;

        public override bool SupportsNL2Fx => _supportsNL2Fx;

        public override bool SupportsFx2NL => _supportsFx2NL;

        public const string ModelIdStr = "Model123";

        // Set on call to NL2Fx 
        public string _log;

        // Set the expected expression to return. 
        public string Expected { get; set; }

        public bool Throw { get; set; }

        public int? Nl2FxDelayTime { get; set; } = null;

        public bool ThrowOnCancellation { get; set; } = false;

        public int PreHandleNl2FxCallCount { get; set; } = 0;

        public TestNL2FxHandler()
        {
        }

        public override async Task<CustomNL2FxResult> NL2FxAsync(NL2FxParameters request, CancellationToken cancel)
        {
            if (Nl2FxDelayTime.HasValue)
            {
                await Task.Delay(Nl2FxDelayTime.Value, CancellationToken.None);
            }

            if (this.Throw)
            {
                throw new InvalidOperationException($"Simulated error");
            }

            if (this.ThrowOnCancellation)
            {
                cancel.ThrowIfCancellationRequested();
            }

            var nl2FxParameters = request;

            Assert.NotNull(nl2FxParameters.Engine);

            var sb = new StringBuilder();
            sb.Append(nl2FxParameters.Sentence);
            sb.Append(": ");

            sb.Append(this.Expected);
            sb.Append(": ");

            foreach (var sym in nl2FxParameters.SymbolSummary.SuggestedSymbols)
            {
                sb.Append($"{sym.BestName},{sym.Type}");
            }

            _log = sb.ToString();

            var nl2FxResult = new CustomNL2FxResult
            {
                Expressions = new CustomNL2FxResultItem[]
                {
                        new CustomNL2FxResultItem
                        {
                            Expression = this.Expected,
                            ModelId = ModelIdStr
                        }
                }
            };

            return nl2FxResult;
        }

        public override void PreHandleNl2Fx(CustomNL2FxParams nl2FxRequestParams, NL2FxParameters nl2fxParameters, LanguageServerOperationContext operationContext)
        {
            this.PreHandleNl2FxCallCount++;
        }
    }

    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData("Score < 50", true, "#$PowerFxResolvedObject$# < #$decimal$#")]
        [InlineData("missing < 50", false, "#$firstname$# < #$decimal$#")] // doesn't compile, should get filtered out by LSP 
        public async Task TestNL2FX(string expectedExpr, bool success, string anonExpr = null)
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
            nl2FxHandler.Nl2FxDelayTime = 100;
            nl2FxHandler.Expected = expectedExpr;

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<CustomNL2FxResult>(rawResponse, payload.id);

            // Assert
            // result has expected concat with symbols. 
            var items = response.Expressions;

            Assert.Single(items);
            var expression = items[0];

            if (anonExpr != null)
            {
                Assert.Equal(anonExpr, items[0].AnonymizedExpression);
            }

            if (success)
            {
                Assert.Equal("my sentence: Score < 50: Score,Number", nl2FxHandler._log);

                Assert.Equal(expectedExpr, expression.Expression);
                Assert.Null(expression.RawExpression);
            }
            else
            {
                // Even though model returned an expression, it didn't compile, so it should be filtered out by LSP.
                Assert.Null(expression.Expression);
                Assert.Equal(expectedExpr, expression.RawExpression);
            }

            Assert.Equal(TestNL2FxHandler.ModelIdStr, expression.ModelId);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }

        [Fact]
        public async Task TestNL2FXMissingHandler()
        {
            // Arrange
            HandlerFactory.SetHandler(CustomProtocolNames.NL2FX, null);
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.MethodNotFound);
        }

        [Fact]
        public async Task TestNL2FXHandlerThrows()
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
            nl2FxHandler.Nl2FxDelayTime = 100;
            nl2FxHandler.Throw = true;

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.InternalError);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
            Assert.Equal(1, nl2FxHandler.PreHandleNl2FxCallCount);
        }

        private static (string payload, string id) NL2FxMessageJson(string documentUri)
        {
            var nl2FxParams = new CustomNL2FxParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1
                },
                Sentence = "my sentence"
            };

            return GetRequestPayload(nl2FxParams, CustomProtocolNames.NL2FX);
        }

        private TestNL2FxHandler CreateAndConfigureNl2FxHandler()
        {
            var nl2FxHandler = new TestNL2FxHandler();
            HandlerFactory.SetHandler(CustomProtocolNames.NL2FX, new Nl2FxLanguageServerOperationHandler(new BackwardsCompatibleNLHandlerFactory(nl2FxHandler)));
            return nl2FxHandler;
        }
    }
}
