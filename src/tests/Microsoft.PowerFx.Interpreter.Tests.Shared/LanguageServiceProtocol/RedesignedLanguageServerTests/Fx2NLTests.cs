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
    internal class TestFx2NlHandler : NLHandler
    {
        public bool _supportsNL2Fx = true;
        public bool _supportsFx2NL = true;

        public override bool SupportsNL2Fx => _supportsNL2Fx;

        public override bool SupportsFx2NL => _supportsFx2NL;

        public const string ModelIdStr = "Model123";

        // Set the expected expression to return. 
        public string Expected { get; set; }

        public bool Throw { get; set; }

        public bool Delay { get; set; } = false;

        public bool SupportsParameterHints { get; set; } = false;

        public TestFx2NlHandler()
        {
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        public override async Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, CancellationToken cancel)
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            if (this.Delay)
            {
                await Task.Delay(100, cancel);
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

        public override async Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, Fx2NLParameters hints, CancellationToken cancel)
        {
            if (!this.SupportsParameterHints)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return await Fx2NLAsync(check, cancel);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            {
                await Task.Delay(100, cancel);
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
    }

    public partial class LanguageServerTestBase
    {
        [Fact]
        public async Task TestFx2NL()
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
            var handler = CreateAndConfigureFx2NlHandler();
            handler.Delay = true;
            handler.Expected = expectedExpr;
            handler.SupportsParameterHints = false;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<CustomFx2NLResult>(rawResponse, payload.id);

            // Assert
            // result has expected concat with symbols. 
            Assert.Equal("Score > 3: True: sentence", response.Explanation);
        }

        [Fact]
        public async Task TestFx2NLUsageHints()
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
            var handler = CreateAndConfigureFx2NlHandler();
            handler.Delay = true;
            handler.Expected = expectedExpr;
            handler.SupportsParameterHints = true;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
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
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            AssertErrorPayload(rawResponse, payload.id, JsonRpcHelper.ErrorCode.MethodNotFound);
        }

        [Fact]
        public async Task TestFx2NLThrows()
        {
            // Arrange
            var documentUri = "powerfx://app?context=1";
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var scope = engine.CreateEditorScope(symbols: symbols);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => scope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var handler = CreateAndConfigureFx2NlHandler();
            handler.Delay = true;
            handler.Throw = true;
            handler.SupportsParameterHints = false;

            // Act
            var payload = Fx2NlMessageJson(documentUri);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);

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

        private TestFx2NlHandler CreateAndConfigureFx2NlHandler()
        {
            var fx2NlHandler = new TestFx2NlHandler();
            HandlerFactory.SetHandler(CustomProtocolNames.FX2NL, new Fx2NlLanguageServerOperationHandler(new BackwardsCompatibleNLHandlerFactory(fx2NlHandler)));
        
            return fx2NlHandler;
        }
    }
}
