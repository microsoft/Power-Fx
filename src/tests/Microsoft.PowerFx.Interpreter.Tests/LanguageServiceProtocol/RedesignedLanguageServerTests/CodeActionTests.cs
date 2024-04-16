// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        private class DummyQuickFixHandler : CodeFixHandler
        {
            public override async Task<IEnumerable<CodeFixSuggestion>> SuggestFixesAsync(Engine engine, CheckResult checkResult, CancellationToken cancel)
            {
                return new CodeFixSuggestion[]
                {
                    new CodeFixSuggestion
                    {
                        SuggestedText = "TestText1",
                        Title = "TestTitle1"
                    }
                };
            }
        }

        // Failure from one handler shouldn't block others. 
        public class ExceptionQuickFixHandler : CodeFixHandler
        {
            public int _counter = 0;

            public override async Task<IEnumerable<CodeFixSuggestion>> SuggestFixesAsync(Engine engine, CheckResult checkResult, CancellationToken cancel)
            {
                _counter++;
                throw new Exception($"expected failure");
            }
        }

        [Fact]
        public async Task TestCodeAction()
        {
            // Arrange
            var failHandler = new ExceptionQuickFixHandler();
            var engine = new Engine(new PowerFxConfig());
            var editor = engine.CreateEditorScope();
            editor.AddQuickFixHandler(new DummyQuickFixHandler());
            editor.AddQuickFixHandler(failHandler);
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => editor);
            Init(new InitParams(scopeFactory: scopeFactory));
            var codeActionParams = new CodeActionParams()
            {
                TextDocument = GetTextDocument("powerfx://test?expression=IsBlank(&context={\"A\":1,\"B\":[1,2,3]}"),
                Range = new LanguageServerProtocol.Protocol.Range()
                {
                    Start = new Position
                    {
                        Line = 0,
                        Character = 0
                    },
                    End = new Position
                    {
                        Line = 0,
                        Character = 10
                    }
                },
                Context = new CodeActionContext() { Only = new[] { CodeActionKind.QuickFix } }
            };
            var payload = GetCodeActionPayload(codeActionParams);

            // Act
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);

            var response = AssertAndGetResponsePayload<Dictionary<string, CodeAction[]>>(rawResponse, payload.id);
            Assert.NotEmpty(response);
            Assert.Contains(CodeActionKind.QuickFix, response.Keys);
            Assert.True(response[CodeActionKind.QuickFix].Length == 1, "Quick fix didn't return expected suggestion.");
            Assert.Equal("TestTitle1", response[CodeActionKind.QuickFix][0].Title);
            Assert.NotEmpty(response[CodeActionKind.QuickFix][0].Edit.Changes);
            Assert.Contains(codeActionParams.TextDocument.Uri, response[CodeActionKind.QuickFix][0].Edit.Changes.Keys);
            Assert.Equal("TestText1", response[CodeActionKind.QuickFix][0].Edit.Changes[codeActionParams.TextDocument.Uri][0].NewText);

            // Fail handler was invokde, but didn't block us. 
            Assert.Equal(1, failHandler._counter); // Invoked
            Assert.Single(TestServer.UnhandledExceptions);
        }

        // Test a codefix using a customization, ICodeFixHandler
        [Fact]
        public async Task TestCodeActionWithHandlerAndExpressionInUriOrInTextDocument()
        {
            // Blank(A) is error, should change to IsBlank(A)
            var original = "Blank(A)";
            var updated = "IsBlank(A)";
            foreach (var addExprToUri in new bool[] { true, false })
            {
                var codeActionParams = new CodeActionParams
                {
                    TextDocument = addExprToUri ?
                                   GetTextDocument(GetUri("context={\"A\":1,\"B\":[1,2,3]}&expression=" + original)) :
                                   GetTextDocument(GetUri("context={\"A\":1,\"B\":[1,2,3]}")),
                    Text = addExprToUri ? null : original,
                    Range = SemanticTokensRelatedTestsHelper.CreateRange(0, 0, 0, 10),
                    Context = GetDefaultCodeActionContext()
                };
                await TestCodeActionWithHandlerCore(codeActionParams, updated).ConfigureAwait(false);
            }

            await TestCodeActionWithHandlerCore(
                new CodeActionParams
                {
                    TextDocument = GetTextDocument(GetUri("context={\"A\":1,\"B\":[1,2,3]}&expression=Max(1,2")),
                    Text = original,
                    Range = SemanticTokensRelatedTestsHelper.CreateRange(0, 0, 0, 10),
                    Context = GetDefaultCodeActionContext()
                }, updated).ConfigureAwait(false);
        }

        private async Task TestCodeActionWithHandlerCore(CodeActionParams codeActionParams, string updatedExpr)
        {
            var engine = new RecalcEngine();
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) =>
            {
                var scope = engine.CreateEditorScope();
                scope.AddQuickFixHandler(new BlankHandler());
                return scope;
            });
            Init(new InitParams(scopeFactory: scopeFactory));

            var payload = GetCodeActionPayload(codeActionParams);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);

            var response = AssertAndGetResponsePayload<Dictionary<string, CodeAction[]>>(rawResponse, payload.id);
            Assert.NotEmpty(response);
            Assert.Contains(CodeActionKind.QuickFix, response.Keys);
            Assert.True(response[CodeActionKind.QuickFix].Length == 1, "Quick fix didn't return expected suggestion.");
            Assert.Equal(BlankHandler.Title, response[CodeActionKind.QuickFix][0].Title);
            Assert.NotEmpty(response[CodeActionKind.QuickFix][0].Edit.Changes);
            Assert.Contains(codeActionParams.TextDocument.Uri, response[CodeActionKind.QuickFix][0].Edit.Changes.Keys);

            Assert.Equal(updatedExpr, response[CodeActionKind.QuickFix][0].Edit.Changes[codeActionParams.TextDocument.Uri][0].NewText);
            Assert.Empty(TestServer.UnhandledExceptions);
        }

        [Fact]
        public async Task TestCodeActionCommandExecuted()
        {
            var engine = new RecalcEngine();

            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) =>
            {
                var scope = engine.CreateEditorScope();
                scope.AddQuickFixHandler(new BlankHandler());
                return scope;
            });
            Init(new InitParams(scopeFactory: scopeFactory));

            var documentUri = "powerfx://test?expression=Blank(A)&context={\"A\":1,\"B\":[1,2,3]}";
            var codeActionsParams1 = new CodeActionParams()
            {
                TextDocument = GetTextDocument(documentUri),
                Range = new LanguageServerProtocol.Protocol.Range()
                {
                    Start = new Position
                    {
                        Line = 0,
                        Character = 0
                    },
                    End = new Position
                    {
                        Line = 0,
                        Character = 10
                    }
                },
                Context = new CodeActionContext() { Only = new[] { CodeActionKind.QuickFix } }
            };
            var payload = GetCodeActionPayload(codeActionsParams1);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload).ConfigureAwait(false);
            var response = AssertAndGetResponsePayload<Dictionary<string, CodeAction[]>>(rawResponse, payload.id);
            Assert.NotEmpty(response);

            var codeActionResult = response[CodeActionKind.QuickFix][0];

            Assert.NotNull(codeActionResult);
            Assert.NotNull(codeActionResult.ActionResultContext);
            Assert.Equal(typeof(BlankHandler).FullName, codeActionResult.ActionResultContext.HandlerName);
            Assert.Equal("Suggestion", codeActionResult.ActionResultContext.ActionIdentifier);
            var commandExecutedParams = new CommandExecutedParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = documentUri
                },
                Command = CommandName.CodeActionApplied,
                Argument = JsonRpcHelper.Serialize(codeActionResult)
            };
            var commandExecutedPayload = GetRequestPayload(commandExecutedParams, CustomProtocolNames.CommandExecuted);
            rawResponse = await TestServer.OnDataReceivedAsync(commandExecutedPayload.payload).ConfigureAwait(false);
            Assert.True(string.IsNullOrEmpty(rawResponse));
            
            commandExecutedParams = new CommandExecutedParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = documentUri
                },
                Command = CommandName.CodeActionApplied,
                Argument = string.Empty
            };
            commandExecutedPayload = GetRequestPayload(commandExecutedParams, CustomProtocolNames.CommandExecuted);
            rawResponse = await TestServer.OnDataReceivedAsync(commandExecutedPayload.payload).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, commandExecutedPayload.id, JsonRpcHelper.ErrorCode.PropertyValueRequired);

            codeActionResult.ActionResultContext = null;
            commandExecutedParams = new CommandExecutedParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = documentUri
                },
                Command = CommandName.CodeActionApplied,
                Argument = JsonRpcHelper.Serialize(codeActionResult)
            };
            commandExecutedPayload = GetRequestPayload(commandExecutedParams, CustomProtocolNames.CommandExecuted);
            rawResponse = await TestServer.OnDataReceivedAsync(commandExecutedPayload.payload).ConfigureAwait(false);
            AssertErrorPayload(rawResponse, commandExecutedPayload.id, JsonRpcHelper.ErrorCode.PropertyValueRequired);
        }

        private static CodeActionContext GetDefaultCodeActionContext()
        {
            return new CodeActionContext() { Only = new[] { CodeActionKind.QuickFix } };
        }

        private static (string payload, string id) GetCodeActionPayload(CodeActionParams codeActionParams)
        {
            return GetRequestPayload(codeActionParams, TextDocumentNames.CodeAction);
        }
    }
}
