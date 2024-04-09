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
    internal class TestFx2NlHandler : BaseFx2NlLanguageServerOperationHandler
    {
        public const string ModelIdStr = "Model123";

        // Set the expected expression to return. 
        public string Expected { get; set; }

        public bool Throw { get; set; }

        public bool Delay { get; set; } = false;

        public bool SupportsParameterHints { get; set; } = false;

        public async Task<CustomFx2NLResult> Fx2NlAsync(CheckResult check, CancellationToken cancellationToken)
        {
            if (this.Delay)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            if (this.Throw)
            {
                throw new InvalidOperationException($"Simulated error");
            }

            var sb = new StringBuilder();
            sb.Append(check.ApplyParse().Text);
            sb.Append(": ");
            sb.Append(check.IsSuccess);
            sb.Append(": ");
            sb.Append(this.Expected);

            var result = new CustomFx2NLResult
            {
                Explanation = sb.ToString()
            };

            return result;
        }

        public async Task<CustomFx2NLResult> Fx2NlAsync(CheckResult check, Fx2NLParameters hints, CancellationToken cancel)
        {
            if (this.Delay)
            {
                await Task.Delay(100, cancel).ConfigureAwait(false);
            }

            if (this.Throw)
            {
                throw new InvalidOperationException($"Simulated error");
            }

            var sb = new StringBuilder();
            sb.Append(hints?.UsageHints?.ControlName);
            sb.Append("; ");
            sb.Append(hints?.UsageHints?.ControlKind);

            sb.Append("; ");
            sb.Append(hints?.UsageHints?.PropertyName);

            return new CustomFx2NLResult
            {
                Explanation = sb.ToString()
            };
        }

        protected override async Task<Fx2NlHandleContext> Fx2NlAsync(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext, CancellationToken cancellationToken)
        {
            var result = await (SupportsParameterHints ?
                                Fx2NlAsync(handleContext.preHandleResult.checkResult, handleContext.preHandleResult.parameters, cancellationToken) :
                                Fx2NlAsync(handleContext.preHandleResult.checkResult, cancellationToken)).ConfigureAwait(false);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, result);
            return handleContext;
        }
    }

    internal class BackwardsCompatTestFx2NlHandler : NLHandler
    {
        public bool _supportsNL2Fx = true;
        public bool _supportsFx2NL = true;

        public override bool SupportsNL2Fx => _supportsNL2Fx;

        public override bool SupportsFx2NL => _supportsFx2NL;

        private readonly TestFx2NlHandler _fx2NlHandler;

        public BackwardsCompatTestFx2NlHandler(TestFx2NlHandler fx2NlHandler)
        {
            _fx2NlHandler = fx2NlHandler;
        }

        public override Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, Fx2NLParameters hints, CancellationToken cancel)
        {
            if (!_fx2NlHandler.SupportsParameterHints)
            {
                return _fx2NlHandler.Fx2NlAsync(check, cancel);
            }

            return _fx2NlHandler.Fx2NlAsync(check, hints, cancel);
        }
    }

    public partial class LanguageServerTestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFx2NL(bool targetBackwardsCompat)
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var expectedExpr = "sentence";

            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var handler = CreateAndConfigureFx2NlHandler(targetBackwardsCompat);
            handler.Delay = true;
            handler.Expected = expectedExpr;
            handler.SupportsParameterHints = false;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<CustomFx2NLResult>(rawResponse, payload.id);

            // Assert
            // result has expected concat with symbols. 
            Assert.Equal("Score > 3: True: sentence", response.Explanation);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFx2NLUsageHints(bool targetBackwardsCompat)
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var expectedExpr = "sentence";

            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = new EditorContextScope(engine, null, symbols)
            {
                UsageHints = new UsageHints
                {
                    ControlKind = "Button",
                    ControlName = "MyButton",
                    PropertyName = "Select"
                }
            }; 
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var handler = CreateAndConfigureFx2NlHandler(targetBackwardsCompat);
            handler.Delay = true;
            handler.Expected = expectedExpr;
            handler.SupportsParameterHints = true;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<CustomFx2NLResult>(rawResponse, payload.id);

            // Assert
            // result has expected concat with symbols. 
            Assert.Equal("MyButton; Button; Select", response.Explanation);
        }

        [Fact]
        public async Task TestFx2NlMissingHandler()
        {
            // Arrange
            HandlerFactory.SetHandler(CustomProtocolNames.FX2NL, null);
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.MethodNotFound);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFx2NLThrows(bool targetBackwardsCompat)
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var handler = CreateAndConfigureFx2NlHandler(targetBackwardsCompat);
            handler.Delay = true;
            handler.Throw = true;
            handler.SupportsParameterHints = false;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);

            // Assert
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.InternalError);
            Assert.NotEmpty(TestServer.UnhandledExceptions);
        }

        private static (string payload, string id) Fx2NlMessageJson(string documentUri, string context = null)
        {
            var fx2NlParams = new CustomFx2NLParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1
                },
                Expression = "Score > 3",
                Context = context
            };

            return GetRequestPayload(fx2NlParams, CustomProtocolNames.FX2NL);
        }

        private TestFx2NlHandler CreateAndConfigureFx2NlHandler(bool targetBackwardsCompat)
        {
            var fx2NlHandler = new TestFx2NlHandler();
            if (targetBackwardsCompat)
            {
                HandlerFactory.SetHandler(CustomProtocolNames.FX2NL, new BackwardsCompatibleFx2NlLanguageServerOperationHandler(new BackwardsCompatibleNLHandlerFactory(new BackwardsCompatTestFx2NlHandler(fx2NlHandler))));
            }
            else
            {
                HandlerFactory.SetHandler(CustomProtocolNames.FX2NL, fx2NlHandler);
            }

            return fx2NlHandler;
        }
    }
}
