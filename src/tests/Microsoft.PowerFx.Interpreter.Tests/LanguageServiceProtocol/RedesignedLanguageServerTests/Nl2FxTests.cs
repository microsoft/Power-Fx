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
    internal class TestNl2FxHandler : BaseNl2FxLanguageServerOperationHandler
    {
        public const string ModelIdStr = "Model123";

        // Set on call to NL2Fx 
        public string _log;

        // Set the expected expression to return. 
        public string Expected { get; set; }

        public bool Throw { get; set; }

        public bool Delay { get; set; } = false;

        protected override async Task<Nl2FxHandleContext> Nl2FxAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext, CancellationToken cancellationToken)
        {
            if (this.Delay)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            if (this.Throw)
            {
                throw new InvalidOperationException($"Simulated error");
            }

            var nl2FxParameters = handleContext.preHandleResult.parameters;

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
            return handleContext with { nl2FxResult = new Nl2FxResult(nl2FxResult) };
        }

        public async Task<CustomNL2FxResult> Nl2FxAsync(NL2FxParameters nL2FxParameters, CancellationToken cancellationToken)
        {
            var context = new Nl2FxHandleContext(null, new Nl2FxPreHandleResult(nL2FxParameters), null);
            var result = await Nl2FxAsync(null, context, cancellationToken).ConfigureAwait(false);
            return result.nl2FxResult.actualResult;
        }
    }

    internal class BackwardsCompatTestNL2FxHandler : NLHandler
    {
        public bool _supportsNL2Fx = true;
        public bool _supportsFx2NL = true;

        public override bool SupportsNL2Fx => _supportsNL2Fx;

        public override bool SupportsFx2NL => _supportsFx2NL;

        private readonly TestNl2FxHandler _nl2FxHandler;

        public BackwardsCompatTestNL2FxHandler(TestNl2FxHandler nl2FxHandler)
        {
            _nl2FxHandler = nl2FxHandler;
        }

        public override async Task<CustomNL2FxResult> NL2FxAsync(NL2FxParameters request, CancellationToken cancel)
        {
           return await _nl2FxHandler.Nl2FxAsync(request, cancel).ConfigureAwait(false);
        }
    }

    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData("Score < 50", true, true, "#$PowerFxResolvedObject$# < #$decimal$#")]
        [InlineData("missing < 50", false, true, "#$firstname$# < #$decimal$#")] // doesn't compile, should get filtered out by LSP 
        [InlineData("Score < 50", true, false, "#$PowerFxResolvedObject$# < #$decimal$#")]
        [InlineData("missing < 50", false, false)]
        public async Task TestNL2FX(string expectedExpr, bool success, bool targetBackwardsCompat, string anonExpr = null)
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var nl2FxHandler = CreateAndConfigureNl2FxHandler(targetBackwardsCompat);
            nl2FxHandler.Delay = true;
            nl2FxHandler.Expected = expectedExpr;

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
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

            Assert.Equal(TestNl2FxHandler.ModelIdStr, expression.ModelId);
        }

        [Fact]
        public async Task TestNL2FXMissingHandler()
        {
            // Arrange
            HandlerFactory.SetHandler(CustomProtocolNames.NL2FX, null);
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.MethodNotFound);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestNL2FXHandlerThrows(bool targetBackwardsCompat)
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var nl2FxHandler = CreateAndConfigureNl2FxHandler(targetBackwardsCompat);
            nl2FxHandler.Delay = true;
            nl2FxHandler.Throw = true;

            // Act
            var payload = NL2FxMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.InternalError);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
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

        private TestNl2FxHandler CreateAndConfigureNl2FxHandler(bool targetBackwardsCompat)
        {
            var nl2FxHandler = new TestNl2FxHandler();
            if (targetBackwardsCompat)
            {
                HandlerFactory.SetHandler(CustomProtocolNames.NL2FX, new BackwardsCompatibleNl2FxLanguageServerOperationHandler(new BackwardsCompatibleNLHandlerFactory(new BackwardsCompatTestNL2FxHandler(nl2FxHandler))));
            }
            else
            {
                HandlerFactory.SetHandler(CustomProtocolNames.NL2FX, nl2FxHandler);
            }

            return nl2FxHandler;
        }
    }
}
