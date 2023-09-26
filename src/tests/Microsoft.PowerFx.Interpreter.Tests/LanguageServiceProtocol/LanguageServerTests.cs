// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Interpreter.Tests.Helpers;
using Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class LanguageServerTests : PowerFxTest
    {
        protected static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#pragma warning disable CS0618 // Type or member is obsolete
            Converters = { new LanguageServerProtocol.FormulaTypeJsonConverter() }
#pragma warning restore CS0618
        };

        protected List<string> _sendToClientData;
        protected TestPowerFxScopeFactory _scopeFactory;
        protected TestLanguageServer _testServer;
        private readonly ITestOutputHelper _output;
        private readonly ConcurrentBag<Exception> _exList = new ConcurrentBag<Exception>();

        public LanguageServerTests(ITestOutputHelper output)
            : base()
        {
            _output = output;
            Init();
        }

        private void Init(Features features = null, ParserOptions options = null)
        {
            var config = new PowerFxConfig(features: features ?? Features.None);
            config.AddFunction(new BehaviorFunction());

            var engine = new Engine(config);

            _sendToClientData = new List<string>();
            _scopeFactory = new TestPowerFxScopeFactory(
                (string documentUri) => engine.CreateEditorScope(options, GetFromUri(documentUri)));
            _testServer = new TestLanguageServer(_output, _sendToClientData.Add, _scopeFactory);
            _testServer.LogUnhandledExceptionHandler += (Exception ex) => _exList.Add(ex);
        }

        // The convention for getting the context from the documentUri is arbitrary and determined by the host. 
        private static ReadOnlySymbolTable GetFromUri(string documentUri)
        {
            var uriObj = new Uri(documentUri);
            var json = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            json ??= "{}";

            var record = (RecordValue)FormulaValueJSON.FromJson(json);
            return ReadOnlySymbolTable.NewFromRecord(record.Type);
        }

        // From JPC spec: https://microsoft.github.io/language-server-protocol/specifications/specification-3-14/
        private const int ParseError = -32700;
        private const int InvalidRequest = -32600;
        private const int MethodNotFound = -32601;
        private const int InvalidParams = -32602;
        private const int InternalError = -32603;
        private const int ServerErrorStart = -32099;
        private const int ServerErrorEnd = -32000;
        private const int ServerNotInitialized = -32002;
        private const int UnknownErrorCode = -32001;
        private const int PropertyValueRequired = -32604;

        [Fact]
        public void TestTopParseError()
        {
            var list = new List<Exception>();

            _testServer.LogUnhandledExceptionHandler += (ex) =>
            {
                list.Add(ex);
            };
            _testServer.OnDataReceived("parse error");

            Assert.Single(list); // ensure handler was invoked. 

            Assert.Single(_sendToClientData);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InternalError, errorResponse.Error.Code);
            Assert.Equal(list[0].GetDetailedExceptionMessage(), errorResponse.Error.Message);
        }

        // Scope facotry that throws. simulate server crashes.
        private class ErrorScopeFactory : IPowerFxScopeFactory
        {
            public IPowerFxScope GetOrCreateInstance(string documentUri)
            {
                throw new NotImplementedException();
            }
        }

        // Exceptions can be thrown oob, test we can register a hook and receive.
        // Check for exceptions if the scope object we call back to throws
        [Fact]
        public void TestLogCallbackExceptions()
        {
            var scopeFactory = new ErrorScopeFactory();
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            var list = new List<Exception>();

            testServer.LogUnhandledExceptionHandler += (ex) =>
            {
                list.Add(ex);
            };

            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didOpen",
                @params = new DidOpenTextDocumentParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = "https://none",
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = "123"
                    }
                }
            }));

            Assert.Single(list); // ensure handler was invoked. 

            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InternalError, errorResponse.Error.Code);
            Assert.Equal(list[0].GetDetailedExceptionMessage(), errorResponse.Error.Message);
        }

        [Fact]
        public void TestLanguageServerCommunication()
        {
            // bad payload
            _testServer.OnDataReceived(JsonSerializer.Serialize(new { }));

            // bad jsonrpc payload
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0"
            }));

            // bad notification payload
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "unknown",
                @params = "unkown"
            }));

            // bad request payload
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "abc",
                method = "unknown",
                @params = "unkown"
            }));

            // verify we have 4 json rpc responeses
            Assert.Equal(4, _sendToClientData.Count);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InvalidRequest, errorResponse.Error.Code);

            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InvalidRequest, errorResponse.Error.Code);

            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[2], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(MethodNotFound, errorResponse.Error.Code);

            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[3], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("abc", errorResponse.Id);
            Assert.Equal(MethodNotFound, errorResponse.Error.Code);

            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("A+CountRows(B)", false)]
        [InlineData("Behavior(); A+CountRows(B)", true)]
        public void TestDidChange(string text, bool withAllowSideEffects)
        {
            Init(options: GetParserOptions(withAllowSideEffects));

            // test good formula
            _sendToClientData.Clear();

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didChange",
                    @params = new DidChangeTextDocumentParams()
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
                    }
                }));

            Assert.Single(_sendToClientData);
            var notification = JsonSerializer.Deserialize<JsonRpcPublishDiagnosticsNotification>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", notification.Jsonrpc);
            Assert.Equal("textDocument/publishDiagnostics", notification.Method);
            Assert.Equal("powerfx://app?context={\"A\":1,\"B\":[1,2,3]}", notification.Params.Uri);
            Assert.Empty(notification.Params.Diagnostics);

            // test bad formula
            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didChange",
                @params = new DidChangeTextDocumentParams()
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
                }
            }));
            Assert.Single(_sendToClientData);
            notification = JsonSerializer.Deserialize<JsonRpcPublishDiagnosticsNotification>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", notification.Jsonrpc);
            Assert.Equal("textDocument/publishDiagnostics", notification.Method);
            Assert.Equal("powerfx://app", notification.Params.Uri);
            Assert.Single(notification.Params.Diagnostics);
            Assert.Equal("Name isn't valid. 'AA' isn't recognized.", notification.Params.Diagnostics[0].Message);

            // some invalid cases
            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new { }));
            Assert.Single(_sendToClientData);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InvalidRequest, errorResponse.Error.Code);

            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didChange"
            }));
            Assert.Single(_sendToClientData);
            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(InvalidRequest, errorResponse.Error.Code);

            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didChange",
                @params = string.Empty
            }));
            Assert.Single(_sendToClientData);
            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Null(errorResponse.Id);
            Assert.Equal(ParseError, errorResponse.Error.Code);

            Assert.Empty(_exList);
        }

        private static ParserOptions GetParserOptions(bool withAllowSideEffects)
        {
            return withAllowSideEffects ? new ParserOptions() { AllowsSideEffects = true } : null;
        }

        private void TestPublishDiagnostics(string uri, string method, string formula, Diagnostic[] expectedDiagnostics)
        {
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method,
                @params = new DidOpenTextDocumentParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = uri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = formula
                    }
                }
            }));
            Assert.Single(_sendToClientData);
            var notification = JsonSerializer.Deserialize<JsonRpcPublishDiagnosticsNotification>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", notification.Jsonrpc);
            Assert.Equal("textDocument/publishDiagnostics", notification.Method);
            Assert.Equal(uri, notification.Params.Uri);
            Assert.Equal(expectedDiagnostics.Length, notification.Params.Diagnostics.Length);

            var diagnosticsSet = new HashSet<Diagnostic>(expectedDiagnostics);
            for (var i = 0; i < expectedDiagnostics.Length; i++)
            {
                var expectedDiagnostic = expectedDiagnostics[i];
                var actualDiagnostic = notification.Params.Diagnostics[i];
                Assert.True(diagnosticsSet.Where(x => x.Message == actualDiagnostic.Message).Count() == 1);
                diagnosticsSet.RemoveWhere(x => x.Message == actualDiagnostic.Message);
            }

            Assert.True(diagnosticsSet.Count() == 0);
            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("A+CountRows(B)", "{\"A\":1,\"B\":[1,2,3]}")]
        public void TestDidOpenValidFormula(string formula, string context = null)
        {
            var uri = $"powerfx://app{(context != null ? "powerfx://app?context=" + context : string.Empty)}";
            TestPublishDiagnostics(uri, "textDocument/didOpen", formula, new Diagnostic[0]);
        }

        [Theory]
        [InlineData("AA", "Name isn't valid. 'AA' isn't recognized.")]
        [InlineData("1+CountRowss", "Name isn't valid. 'CountRowss' isn't recognized.")]
        [InlineData("CountRows(2)", "Invalid argument type (Decimal). Expecting a Table value instead.", "The function 'CountRows' has some invalid arguments.")]
        public void TestDidOpenErroneousFormula(string formula, params string[] expectedErrors)
        {
            var expectedDiagnostics = expectedErrors.Select(error => new Diagnostic()
            {
                Message = error,
                Severity = DiagnosticSeverity.Error
            }).ToArray();
            TestPublishDiagnostics("powerfx://app", "textDocument/didOpen", formula, expectedDiagnostics);

            Assert.Empty(_exList);
        }

        [Fact]
        public void TestDidOpenSeverityFormula()
        {
            var formula = "Count([\"test\"])";
            var expectedDiagnostics = new[]
            {
                new Diagnostic()
                {
                    Message = "Invalid schema, expected a column of Number values for 'Value'.",
                    Severity = DiagnosticSeverity.Warning
                },
                new Diagnostic()
                {
                    Message = "The function 'Count' has some invalid arguments.",
                    Severity = DiagnosticSeverity.Error
                },
            };
            TestPublishDiagnostics("powerfx://app", "textDocument/didOpen", formula, expectedDiagnostics);
        }

        [Theory]
        [InlineData("Concatenate(", 12, false, false)]
        [InlineData("Behavior(); Concatenate(", 24, true, false)]
        [InlineData("Behavior(); Concatenate(", 24, false, true)]
        public void TestDidOpenWithErrors(string text, int offset, bool withAllowSideEffects, bool expectBehaviorError)
        {
            Init(options: GetParserOptions(withAllowSideEffects));

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didOpen",
                    @params = new DidOpenTextDocumentParams()
                    {
                        TextDocument = new TextDocumentItem()
                        {
                            Uri = "powerfx://app",
                            LanguageId = "powerfx",
                            Version = 1,
                            Text = text
                        }
                    }
                }));

            CheckBehaviorError(_sendToClientData[0], expectBehaviorError, out var diags);

            var diag = diags.First(d => d.Message == "Unexpected characters. The formula contains 'Eof' where 'ParenClose' is expected.");

            Assert.Equal(offset, diag.Range.Start.Character);
            Assert.Equal(offset, diag.Range.End.Character);

            Assert.Empty(_exList);
        }

        private void CheckBehaviorError(string sentToClientData, bool expectBehaviorError, out Diagnostic[] diags)
        {
            diags = JsonSerializer.Deserialize<JsonRpcPublishDiagnosticsNotification>(sentToClientData, _jsonSerializerOptions).Params.Diagnostics;

            if (expectBehaviorError)
            {
                Assert.Contains(diags, d => d.Message == StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag));
            }
            else
            {
                Assert.DoesNotContain(diags, d => d.Message == StringResources.GetErrorResource(TexlStrings.ErrBehaviorPropertyExpected).GetSingleValue(ErrorResource.ShortMessageTag));
            }
        }

        [Theory]
        [InlineData("Color.AliceBl", 13, false)]
        [InlineData("Behavior(); Color.AliceBl", 25, true)]

        // $$$ This test generates an internal error as we use an behavior function but we have no way to check its presence
        [InlineData("Behavior(); Color.AliceBl", 25, false)]
        public void TestCompletionWithExpressionInUriOrNotInUri(string text, int offset, bool withAllowSideEffects)
        {
            foreach (var addExprToUri in new bool[] { true, false })
            {
                var params1 = new CompletionParams()
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text)) : GetTextDocument(),
                    Text = addExprToUri ? null : text,
                    Position = GetPosition(offset),
                    Context = GetCompletionContext()
                };
                var params2 = new CompletionParams()
                {
                    TextDocument = addExprToUri ? 
                                   GetTextDocument(GetUri("expression=Color.&context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}")) : 
                                   GetTextDocument(GetUri("context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}")),
                    Text = addExprToUri ? null : "Color.",
                    Position = GetPosition(7),
                    Context = GetCompletionContext()
                };
                var params3 = new CompletionParams()
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression={a:{},b:{},c:{}}.")) : GetTextDocument(),
                    Text = addExprToUri ? null : "{a:{},b:{},c:{}}.",
                    Position = GetPosition(17),
                    Context = GetCompletionContext()
                };
                TestCompletionCore(params1, params2, params3, withAllowSideEffects);
            }
        }

        [Fact]
        public void TestCompletionWithExpressionBothInUriAndTextDocument()
        {
            int offset = 25;
            string text = "Behavior(); Color.AliceBl";
            string uriText = "Max(1,";
            bool withAllowSideEffects = false;

            var params1 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=" + uriText)),
                Text = text,
                Position = GetPosition(offset),
                Context = GetCompletionContext()
            };
            var params2 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}&expression=1+1")),
                Text = "Color.",
                Position = GetPosition(7),
                Context = GetCompletionContext()
            };
            var params3 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=Color.")),
                Text = "{a:{},b:{},c:{}}.",
                Position = GetPosition(17),
                Context = GetCompletionContext()
            };
            TestCompletionCore(params1, params2, params3, withAllowSideEffects);
        }

        private void TestCompletionCore(CompletionParams params1, CompletionParams params2, CompletionParams params3, bool withAllowSideEffects)
        {
            Init(options: GetParserOptions(withAllowSideEffects));

            // test good formula
            _sendToClientData.Clear();
            var payload = GetCompletionPayload(params1);
            _testServer.OnDataReceived(payload.payload);
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            var foundItems = response.Result.Items.Where(item => item.Label == "AliceBlue");
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");
            Assert.True(foundItems.First().InsertText == "AliceBlue");
            Assert.True(foundItems.First().SortText == "0");

            _sendToClientData.Clear();
            payload = GetCompletionPayload(params2);
            _testServer.OnDataReceived(payload.payload);
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            foundItems = response.Result.Items.Where(item => item.Label == "AliceBlue");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");
            Assert.True(foundItems.First().InsertText == "AliceBlue");
            Assert.True(foundItems.First().SortText == "0");

            _sendToClientData.Clear();
            payload = GetCompletionPayload(params3);
            _testServer.OnDataReceived(payload.payload);
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);

            foundItems = response.Result.Items.Where(item => item.Label == "a");
            Assert.True(Enumerable.Count(foundItems) == 1, "'a' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(foundItems.First().InsertText == "a");
            Assert.True(foundItems.First().SortText == "0");

            foundItems = response.Result.Items.Where(item => item.Label == "b");
            Assert.True(Enumerable.Count(foundItems) == 1, "'b' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(foundItems.First().InsertText == "b");
            Assert.True(foundItems.First().SortText == "1");

            foundItems = response.Result.Items.Where(item => item.Label == "c");
            Assert.True(Enumerable.Count(foundItems) == 1, "'c' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(foundItems.First().InsertText == "c");
            Assert.True(foundItems.First().SortText == "2");

            // missing 'expression' in documentUri
            _sendToClientData.Clear();
            payload = GetCompletionPayload(new CompletionParams()
            {
                TextDocument = GetTextDocument("powerfx://test"),
                Position = GetPosition(1),
                Context = GetCompletionContext()
            });
            _testServer.OnDataReceived(payload.payload); 
            Assert.Single(_sendToClientData);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal(payload.id, errorResponse.Id);
            Assert.Equal(InvalidParams, errorResponse.Error.Code);

            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("'A", 1)]
        [InlineData("'Acc", 1)]
        public void TestCompletionWithIdentifierDelimiter(string text, int offset)
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockDataSourceEngine());
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            var params1 = new CompletionParams()
            {
                TextDocument = GetTextDocument(GetUri("expression=" + text)),
                Text = text,
                Position = GetPosition(offset),
                Context = GetCompletionContext()
            };
            _sendToClientData.Clear();
            var payload = GetCompletionPayload(params1);
            testServer.OnDataReceived(payload.payload);
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);

            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            var foundItems = response.Result.Items.Where(item => item.Label == "'Account'");
            Assert.True(Enumerable.Count(foundItems) == 1, "'Account' should be found from suggestion result");

            // Test that the Identifier delimiter is ignored in case of insertText,
            // when preceding character is also the same identifier delimiter
            Assert.True(foundItems.First().InsertText == "Account'");
            Assert.True(foundItems.First().SortText == "0");
        }

        private static (string payload, string id) GetCompletionPayload(CompletionParams completionParams)
        {
            return GetRequestPayload(completionParams, TextDocumentNames.Completion);
        }

        private static CompletionContext GetCompletionContext()
        {
            return new CompletionContext();
        }

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
        public void TestCodeAction()
        {
            var failHandler = new ExceptionQuickFixHandler();
            var engine = new Engine(new PowerFxConfig());
            var editor = engine.CreateEditorScope();
            editor.AddQuickFixHandler(new DummyQuickFixHandler());
            editor.AddQuickFixHandler(failHandler);

            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => editor);
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);

            var errorList = new List<Exception>();

            testServer.LogUnhandledExceptionHandler += (ex) =>
            {
                errorList.Add(ex);
            };

            var documentUri = "powerfx://test?expression=IsBlank(&context={\"A\":1,\"B\":[1,2,3]}";

            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = TextDocumentNames.CodeAction,
                @params = new CodeActionParams()
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
                }
            }));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCodeActionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("testDocument1", response.Id);
            Assert.NotEmpty(response.Result);
            Assert.Contains(CodeActionKind.QuickFix, response.Result.Keys);
            Assert.True(response.Result[CodeActionKind.QuickFix].Length == 1, "Quick fix didn't return expected suggestion.");
            Assert.Equal("TestTitle1", response.Result[CodeActionKind.QuickFix][0].Title);
            Assert.NotEmpty(response.Result[CodeActionKind.QuickFix][0].Edit.Changes);
            Assert.Contains(documentUri, response.Result[CodeActionKind.QuickFix][0].Edit.Changes.Keys);
            Assert.Equal("TestText1", response.Result[CodeActionKind.QuickFix][0].Edit.Changes[documentUri][0].NewText);

            // Fail handler was invokde, but didn't block us. 
            Assert.Equal(1, failHandler._counter); // Invoked
            Assert.Single(errorList);
        }

        // Test a codefix using a customization, ICodeFixHandler
        [Fact]
        public void TestCodeActionWithHandlerAndExpressionInUriOrInTextDocument()
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
                TestCodeActionWithHandlerCore(codeActionParams, updated);
            }

            TestCodeActionWithHandlerCore(
                new CodeActionParams
            {
                TextDocument = GetTextDocument(GetUri("context={\"A\":1,\"B\":[1,2,3]}&expression=Max(1,2")),
                Text = original,
                Range = SemanticTokensRelatedTestsHelper.CreateRange(0, 0, 0, 10),
                Context = GetDefaultCodeActionContext()
            }, updated);
        }

        private void TestCodeActionWithHandlerCore(CodeActionParams codeActionParams, string updatedExpr)
        {
            var engine = new RecalcEngine();
            _sendToClientData.Clear();
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) =>
            {
                var scope = engine.CreateEditorScope();
                scope.AddQuickFixHandler(new BlankHandler());
                return scope;
            });

            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            var payload = GetCodeActionPayload(codeActionParams);
            testServer.OnDataReceived(payload.payload);

            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCodeActionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            Assert.NotEmpty(response.Result);
            Assert.Contains(CodeActionKind.QuickFix, response.Result.Keys);
            Assert.True(response.Result[CodeActionKind.QuickFix].Length == 1, "Quick fix didn't return expected suggestion.");
            Assert.Equal(BlankHandler.Title, response.Result[CodeActionKind.QuickFix][0].Title);
            Assert.NotEmpty(response.Result[CodeActionKind.QuickFix][0].Edit.Changes);
            Assert.Contains(codeActionParams.TextDocument.Uri, response.Result[CodeActionKind.QuickFix][0].Edit.Changes.Keys);

            Assert.Equal(updatedExpr, response.Result[CodeActionKind.QuickFix][0].Edit.Changes[codeActionParams.TextDocument.Uri][0].NewText);
            Assert.Empty(exList);
        }

        private static CodeActionContext GetDefaultCodeActionContext()
        {
            return new CodeActionContext() { Only = new[] { CodeActionKind.QuickFix } };
        }

        private static (string payload, string id) GetCodeActionPayload(CodeActionParams codeActionParams)
        {
            return GetRequestPayload(codeActionParams, TextDocumentNames.CodeAction);
        }

        [Fact]
        public void TestCodeActionCommandExecuted()
        {
            var engine = new RecalcEngine();

            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) =>
            {
                var scope = engine.CreateEditorScope();
                scope.AddQuickFixHandler(new BlankHandler());
                return scope;
            });

            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            var documentUri = "powerfx://test?expression=Blank(A)&context={\"A\":1,\"B\":[1,2,3]}";

            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = TextDocumentNames.CodeAction,
                @params = new CodeActionParams()
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
                }
            }));

            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCodeActionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("testDocument1", response.Id);
            Assert.NotEmpty(response.Result);

            var codeActionResult = response.Result[CodeActionKind.QuickFix][0];

            Assert.NotNull(codeActionResult);
            Assert.NotNull(codeActionResult.ActionResultContext);
            Assert.Equal(typeof(BlankHandler).FullName, codeActionResult.ActionResultContext.HandlerName);
            Assert.Equal("Suggestion", codeActionResult.ActionResultContext.ActionIdentifier);

            _sendToClientData.Clear();
            Assert.Empty(exList);

            var testServer1 = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            testServer1.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            testServer1.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = CustomProtocolNames.CommandExecuted,
                @params = new CommandExecutedParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = documentUri
                    },
                    Command = CommandName.CodeActionApplied,
                    Argument = JsonRpcHelper.Serialize(codeActionResult)
                }
            }));

            Assert.Empty(_sendToClientData);

            _sendToClientData.Clear();
            Assert.Empty(exList);

            testServer1 = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            testServer1.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            testServer1.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = CustomProtocolNames.CommandExecuted,
                @params = new CommandExecutedParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = documentUri
                    },
                    Command = CommandName.CodeActionApplied,
                    Argument = string.Empty
                }
            }));

            Assert.Single(_sendToClientData);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("testDocument1", errorResponse.Id);
            Assert.Equal(PropertyValueRequired, errorResponse.Error.Code);

            _sendToClientData.Clear();
            Assert.Empty(exList);

            testServer1 = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            testServer1.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);
            codeActionResult.ActionResultContext = null;
            testServer1.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = CustomProtocolNames.CommandExecuted,
                @params = new CommandExecutedParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = documentUri
                    },
                    Command = CommandName.CodeActionApplied,
                    Argument = JsonRpcHelper.Serialize(codeActionResult)
                }
            }));

            Assert.Single(_sendToClientData);
            errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("testDocument1", errorResponse.Id);
            Assert.Equal(PropertyValueRequired, errorResponse.Error.Code);
            Assert.Empty(exList);
        }

        [Theory]
        [InlineData("{1}", 1)]
        [InlineData("12{3}45", 3)]
        [InlineData("1234{5}", 5)]
        [InlineData("123\n1{2}3", 2)]
        [InlineData("123\n{5}67", 1)]
        [InlineData("123\n5{6}7", 2)]
        [InlineData("123\n56{7}", 3)]
        [InlineData("123\n567{\n}890", 3)]
        public void TestGetCharPosition(string expression, int expected)
        {
            var pattern = @"\{[0-9|\n]\}";
            var re = new Regex(pattern);
            var matches = re.Matches(expression);
            Assert.Single(matches);

            var position = matches[0].Index;
            expression = expression.Substring(0, position) + expression[position + 1] + expression.Substring(position + 3);

            Assert.Equal(expected, _testServer.TestGetCharPosition(expression, position));
            Assert.Empty(_exList);
        }

        [Fact]
        public void TestGetPosition()
        {
            Assert.Equal(0, _testServer.TestGetPosition("123", 0, 0));
            Assert.Equal(1, _testServer.TestGetPosition("123", 0, 1));
            Assert.Equal(2, _testServer.TestGetPosition("123", 0, 2));
            Assert.Equal(4, _testServer.TestGetPosition("123\n123456\n12345", 1, 0));
            Assert.Equal(5, _testServer.TestGetPosition("123\n123456\n12345", 1, 1));
            Assert.Equal(11, _testServer.TestGetPosition("123\n123456\n12345", 2, 0));
            Assert.Equal(13, _testServer.TestGetPosition("123\n123456\n12345", 2, 2));
            Assert.Equal(3, _testServer.TestGetPosition("123", 0, 999));

            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("Power(", 6, "Power(2,", 8, false)]
        [InlineData("Behavior(); Power(", 18, "Behavior(); Power(2,", 20, true)]

        // This tests generates an internal error as we use an behavior function but we have no way to check its presence
        [InlineData("Behavior(); Power(", 18, "Behavior(); Power(2,", 20, false)]
        public void TestSignatureHelpWithExpressionInUriAndNotInUri(string text, int offset, string text2, int offset2, bool withAllowSideEffects)
        {
            foreach (var addExprToUri in new bool[] { true, false })
            {
                var signatureHelpParams1 = new SignatureHelpParams
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text)) : GetTextDocument(),
                    Text = addExprToUri ? null : text,
                    Position = GetPosition(offset),
                    Context = GetSignatureHelpContext("(")
                };
                var signatureHelpParams2 = new SignatureHelpParams
                {
                    TextDocument = addExprToUri ? GetTextDocument(GetUri("expression=" + text2)) : GetTextDocument(),
                    Text = addExprToUri ? null : text2,
                    Position = GetPosition(offset2),
                    Context = GetSignatureHelpContext(",")
                };
                TestSignatureHelpCore(signatureHelpParams1, signatureHelpParams2, withAllowSideEffects);
            }
        }

        [Fact]
        public void TestSignatureHelpWithExpressionInBothUriAndTextDocument()
        {
            var signatureHelpParams1 = new SignatureHelpParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(")),
                Text = "Power(",
                Position = GetPosition(6),
                Context = GetSignatureHelpContext("(")
            };
            var signatureHelpParams2 = new SignatureHelpParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(")),
                Text = "Behavior(); Power(2,",
                Position = GetPosition(20),
                Context = GetSignatureHelpContext(",")
            };
            TestSignatureHelpCore(signatureHelpParams1, signatureHelpParams2, true);
        }

        private void TestSignatureHelpCore(SignatureHelpParams signatureHelpParams1, SignatureHelpParams signatureHelpParams2, bool withAllowSideEffects)
        {
            Init(options: GetParserOptions(withAllowSideEffects));

            // test good formula
            var payload = GetSignatureHelpPayload(signatureHelpParams1);
            _testServer.OnDataReceived(payload.payload);
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcSignatureHelpResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            Assert.Equal(0U, response.Result.ActiveSignature);
            Assert.Equal(0U, response.Result.ActiveParameter);
            var foundItems = response.Result.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) >= 1, "Power should be found from signatures result");
            Assert.Equal(0U, foundItems.First().ActiveParameter);
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            _sendToClientData.Clear();
            payload = GetSignatureHelpPayload(signatureHelpParams2);
            _testServer.OnDataReceived(payload.payload);

            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcSignatureHelpResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal(payload.id, response.Id);
            Assert.Equal(0U, response.Result.ActiveSignature);
            Assert.Equal(1U, response.Result.ActiveParameter);
            foundItems = response.Result.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) >= 1, "Power should be found from signatures result");
            Assert.Equal(0U, foundItems.First().ActiveParameter);
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            // missing 'expression' in documentUri
            _sendToClientData.Clear();
            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = "123",
                    method = "textDocument/signatureHelp",
                    @params = new CompletionParams()
                    {
                        TextDocument = GetTextDocument("powerfx://test"),
                        Position = new Position()
                        {
                            Line = 0,
                            Character = 1
                        },
                        Context = new CompletionContext()
                    }
                }));

            Assert.Single(_sendToClientData);
            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("123", errorResponse.Id);
            Assert.Equal(InvalidParams, errorResponse.Error.Code);
            Assert.Empty(_exList);
        }

        private static (string payload, string id) GetSignatureHelpPayload(SignatureHelpParams signatureHelpParams)
        {
            return GetRequestPayload(signatureHelpParams, TextDocumentNames.SignatureHelp);
        }

        private static Position GetPosition(int offset, int line = 0)
        {
            return new Position()
            {
                Line = line,
                Character = offset
            };
        }

        private static SignatureHelpContext GetSignatureHelpContext(string triggerChar)
        {
            return new SignatureHelpContext
            {
                TriggerKind = SignatureHelpTriggerKind.TriggerCharacter,
                TriggerCharacter = triggerChar
            };
        }

        [Theory]
        [InlineData("A+CountRows(B)", 3, false, false)]
        [InlineData("Behavior(); A+CountRows(B)", 4, true, false)]
        [InlineData("Behavior(); A+CountRows(B)", 4, false, true)]
        public void TestPublishTokens(string text, int count, bool withAllowSideEffects, bool expectBehaviorError)
        {
            Init(options: GetParserOptions(withAllowSideEffects));

            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=1";

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didOpen",
                    @params = new DidOpenTextDocumentParams()
                    {
                        TextDocument = new TextDocumentItem()
                        {
                            Uri = documentUri,
                            LanguageId = "powerfx",
                            Version = 1,
                            Text = text
                        }
                    }
                }));

            Assert.Equal(2, _sendToClientData.Count);
            var response = JsonSerializer.Deserialize<JsonRpcPublishTokensNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishTokens", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Equal(count, response.Params.Tokens.Count);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["A"]);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["B"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["CountRows"]);

            CheckBehaviorError(_sendToClientData[0], expectBehaviorError, out _);

            if (count == 4)
            {
                Assert.Equal(TokenResultType.Function, response.Params.Tokens["Behavior"]);
            }

            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            _sendToClientData.Clear();
            documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=2";

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didChange",
                    @params = new DidChangeTextDocumentParams()
                    {
                        TextDocument = new VersionedTextDocumentIdentifier()
                        {
                            Uri = documentUri,
                            Version = 1,
                        },
                        ContentChanges = new TextDocumentContentChangeEvent[]
                    {
                        new TextDocumentContentChangeEvent() { Text = text }
                    }
                    }
                }));

            Assert.Equal(2, _sendToClientData.Count);
            response = JsonSerializer.Deserialize<JsonRpcPublishTokensNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishTokens", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Equal(0, Enumerable.Count(response.Params.Tokens.Where(it => it.Value != TokenResultType.Function)));
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Abs"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Clock.AmPm"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["CountRows"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["VarP"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Year"]);

            CheckBehaviorError(_sendToClientData[0], expectBehaviorError, out _);

            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            _sendToClientData.Clear();
            documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=3";

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didChange",
                    @params = new DidChangeTextDocumentParams()
                    {
                        TextDocument = new VersionedTextDocumentIdentifier()
                        {
                            Uri = documentUri,
                            Version = 1,
                        },
                        ContentChanges = new TextDocumentContentChangeEvent[]
                    {
                        new TextDocumentContentChangeEvent() { Text = text }
                    }
                    }
                }));

            Assert.Equal(2, _sendToClientData.Count);
            response = JsonSerializer.Deserialize<JsonRpcPublishTokensNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishTokens", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["A"]);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["B"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Abs"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Clock.AmPm"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["CountRows"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["VarP"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["Year"]);

            CheckBehaviorError(_sendToClientData[0], expectBehaviorError, out _);
            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("{\"A\": 1 }", "A+2", typeof(DecimalType))]
        [InlineData("{}", "\"hi\"", typeof(StringType))]
        [InlineData("{}", "", typeof(BlankType))]
        [InlineData("{}", "{ A: 1 }", typeof(KnownRecordType))]
        [InlineData("{}", "[1, 2, 3]", typeof(TableType))]
        [InlineData("{}", "true", typeof(BooleanType))]
        public void TestPublishExpressionType(string context, string expression, System.Type expectedType)
        {
            var documentUri = $"powerfx://app?context={context}&getExpressionType=true";
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didOpen",
                @params = new DidOpenTextDocumentParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = expression
                    }
                }
            }));

            Assert.Equal(2, _sendToClientData.Count);
            var response = JsonSerializer.Deserialize<JsonRpcPublishExpressionTypeNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishExpressionType", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.IsType(expectedType, response.Params.Type);

            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData("{\"A\": 1 }", "invalid+A")]
        [InlineData("{}", "B")]
        [InlineData("{}", "+")]
        public void TestPublishExpressionType_Null(string context, string expression)
        {
            var documentUri = $"powerfx://app?context={context}&getExpressionType=true";
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didOpen",
                @params = new DidOpenTextDocumentParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = expression
                    }
                }
            }));

            Assert.Equal(2, _sendToClientData.Count);
            var response = JsonSerializer.Deserialize<JsonRpcPublishExpressionTypeNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishExpressionType", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Null(response.Params.Type);
            Assert.Empty(_exList);
        }

        [Theory]
        [InlineData(false, "{}", "{ A: 1 }", @"{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Decimal""}}}")]
        [InlineData(false, "{}", "[1, 2]", @"{""Type"":""Table"",""Fields"":{""Value"":{""Type"":""Decimal""}}}")]
        [InlineData(true, "{}", "[{ A: 1 }, { B: true }]", @"{""Type"":""Table"",""Fields"":{""A"":{""Type"":""Decimal""},""B"":{""Type"":""Boolean""}}}")]
        [InlineData(false, "{}", "[{ A: 1 }, { B: true }]", @"{""Type"":""Table"",""Fields"":{""Value"":{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Decimal""},""B"":{""Type"":""Boolean""}}}}}")]
        [InlineData(false, "{}", "{A: 1, B: { C: { D: \"Qwerty\" }, E: true } }", @"{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Decimal""},""B"":{""Type"":""Record"",""Fields"":{""C"":{""Type"":""Record"",""Fields"":{""D"":{""Type"":""String""}}},""E"":{""Type"":""Boolean""}}}}}")]
        [InlineData(false, "{}", "{ type: 123 }", @"{""Type"":""Record"",""Fields"":{""type"":{""Type"":""Decimal""}}}")]
        public void TestPublishExpressionType_AggregateShapes(bool tableSyntaxDoesntWrapRecords, string context, string expression, string expectedTypeJson)
        {
            Init(new Features { TableSyntaxDoesntWrapRecords = tableSyntaxDoesntWrapRecords });
            var documentUri = $"powerfx://app?context={context}&getExpressionType=true";
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "textDocument/didOpen",
                @params = new DidOpenTextDocumentParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = expression
                    }
                }
            }));

            Assert.Equal(2, _sendToClientData.Count);
            var response = JsonSerializer.Deserialize<JsonRpcPublishExpressionTypeNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishExpressionType", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Equal(expectedTypeJson, JsonSerializer.Serialize(response.Params.Type, _jsonSerializerOptions));
            Assert.Empty(_exList);
        }

        [Fact]
        public void TestInitialFixup()
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockSqlEngine());
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);
            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";
            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "$/initialFixup",
                @params = new InitialFixupParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = "new_price * new_quantity"
                    }
                }
            }));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcInitialFixupResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("123", response.Id);
            Assert.Equal(documentUri, response.Result.Uri);
            Assert.Equal("Price * Quantity", response.Result.Text);

            // no change
            _sendToClientData.Clear();
            Assert.Empty(exList);
            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "$/initialFixup",
                @params = new InitialFixupParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = "Price * Quantity"
                    }
                }
            }));
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcInitialFixupResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("123", response.Id);
            Assert.Equal(documentUri, response.Result.Uri);
            Assert.Equal("Price * Quantity", response.Result.Text);
            Assert.Empty(exList);
        }

        public class JsonRpcNL2FxResponse
        {
            public string Jsonrpc { get; set; } = string.Empty;

            public string Id { get; set; } = string.Empty;

            // LSP wraps in another envelope. 
            public Payload Result { get; set; }

            public class PayloadItem
            {
                public string Expression { get; set; }
            }

            public class Payload
            {
                public PayloadItem[] Expressions { get; set; }
            }
        }

        public class JsonRpcFx2NLResponse
        {
            public string Jsonrpc { get; set; } = string.Empty;

            public string Id { get; set; } = string.Empty;

            // LSP wraps in another envelope. 
            public Payload Result { get; set; }

            public class Payload
            {
                public string Explanation { get; set; }
            }            
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, false, true)]
        public void TestGetCapabilities(bool supportNL2Fx, bool supportFx2NL, bool dontRegister = false)
        {
            var documentUri = "powerfx://app?context=1";

            var engine = new Engine();
            var symbols = new SymbolTable();
            var editor = engine.CreateEditorScope(symbols: symbols);

            var dict = new Dictionary<string, EditorContextScope>
            {
                { documentUri, editor }
            };
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => dict[documentUri]);

            var testNLHandler = new TestNLHandler
            {
                 _supportsFx2NL = supportFx2NL,
                 _supportsNL2Fx = supportNL2Fx
            };
            if (dontRegister)
            {
                testNLHandler = null;
            }

            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory)
            {
                NL2FxImplementation = testNLHandler
            };

            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "$/getcapabilities",
                @params = new CustomNL2FxParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1
                    }
                }
            }));

            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcGetCapabilitiesResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("123", response.Id);

            // result has expected concat with symbols. 
            Assert.Equal(supportNL2Fx, response.Result.SupportsNL2Fx);
            Assert.Equal(supportFx2NL, response.Result.SupportsFx2NL);
        }

        public class JsonRpcGetCapabilitiesResponse
        {
            public string Jsonrpc { get; set; } = string.Empty;

            public string Id { get; set; } = string.Empty;

            // LSP wraps in another envelope. 
            public CustomGetCapabilitiesResult Result { get; set; }

            public class CustomGetCapabilitiesResult
            {
                /// <summary>
                /// Supports the $/nl2fx message. 
                /// This is determined by the <see cref="NLHandler"/> registered with the language server. 
                /// </summary>
                public bool SupportsNL2Fx { get; set; }

                /// <summary>
                /// Supports the $/fx2nl message. 
                /// </summary>
                public bool SupportsFx2NL { get; set; }
            }
        }

        private class TestNLHandler : NLHandler
        {
            // Set the expected expression to return. 
            public string Expected { get; set; }

            public bool Throw { get; set; }

            public bool _supportsNL2Fx = true;
            public bool _supportsFx2NL = true;

            public override bool SupportsNL2Fx => _supportsNL2Fx;

            public override bool SupportsFx2NL => _supportsFx2NL;

            public override async Task<CustomNL2FxResult> NL2FxAsync(NL2FxParameters request, CancellationToken cancel)
            {
                if (this.Throw)
                {
                    throw new InvalidOperationException($"Simulated error");
                }

                var sb = new StringBuilder();
                sb.Append(request.Sentence);
                sb.Append(": ");

                sb.Append(this.Expected);
                sb.Append(": ");

                foreach (var sym in request.SymbolSummary.SuggestedSymbols)
                {
                    sb.Append($"{sym.BestName},{sym.Type}");
                }                

                return new CustomNL2FxResult
                {
                    Expressions = new CustomNL2FxResultItem[]
                    {
                        new CustomNL2FxResultItem 
                        {
                            Expression = sb.ToString()
                        }
                    }
                };
            }

            public override async Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, CancellationToken cancel)
            {
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

                return new CustomFx2NLResult
                {
                    Explanation = sb.ToString()
                };
            }
        }

        private static string NL2FxMessageJson(string documentUri)
        {
            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "$/nl2fx",
                @params = new CustomNL2FxParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1
                    },
                    Sentence = "my sentence"
                }
            });
        }

        private static string FX2NlMessageJson(string documentUri)
        {
            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "$/fx2nl",
                @params = new CustomFx2NLParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1
                    },
                    Expression = "Score > 3"
                }
            });
        }

        [Fact]
        public void TestNL2FX()
        {
            var documentUri = "powerfx://app?context=1";
            var expectedExpr = "score < 50";

            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var editor = engine.CreateEditorScope(symbols: symbols);

            var dict = new Dictionary<string, EditorContextScope>
            {
                { documentUri, editor }
            };
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => dict[documentUri]);

            var testNLHandler = new TestNLHandler { Expected = expectedExpr };
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory)
            {
                NL2FxImplementation = testNLHandler 
            };

            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);
            
            testServer.OnDataReceived(NL2FxMessageJson(documentUri));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcNL2FxResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("123", response.Id);

            // result has expected concat with symbols. 
            Assert.Equal("my sentence: score < 50: Score,Number", response.Result.Expressions[0].Expression);
        }

        [Fact]
        public void TestNL2FXMissingHandler()
        {
            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var editor = engine.CreateEditorScope(symbols: symbols);

            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => editor);

            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);

            // No handler registered
            Assert.Null(testServer.NL2FxImplementation);

            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";
            testServer.OnDataReceived(NL2FxMessageJson(documentUri));
            Assert.Single(_sendToClientData);

            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);
            
            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("123", errorResponse.Id);
            Assert.NotNull(errorResponse.Error);
        }

        [Fact]
        public void TestNL2FXHandlerThrows()
        {
            var documentUri = "powerfx://app?context=1";

            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var editor = engine.CreateEditorScope(symbols: symbols);

            var dict = new Dictionary<string, EditorContextScope>
            {
                { documentUri, editor }
            };
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => dict[documentUri]);

            var testNLHandler = new TestNLHandler { Throw = true }; // simulate error
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory)
            {
                NL2FxImplementation = testNLHandler
            };

            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);
            testServer.OnDataReceived(NL2FxMessageJson(documentUri));
            Assert.Single(_sendToClientData);

            var errorResponse = JsonSerializer.Deserialize<JsonRpcErrorResponse>(_sendToClientData[0], _jsonSerializerOptions);

            Assert.Equal("2.0", errorResponse.Jsonrpc);
            Assert.Equal("123", errorResponse.Id);
            Assert.NotNull(errorResponse.Error);
        }

        [Fact]
        public void TestFx2NL()
        {
            var documentUri = "powerfx://app?context=1";
            var expectedExpr = "sentence";

            var engine = new Engine();
            var symbols = new SymbolTable();
            symbols.AddVariable("Score", FormulaType.Number);
            var editor = engine.CreateEditorScope(symbols: symbols);

            var dict = new Dictionary<string, EditorContextScope>
            {
                { documentUri, editor }
            };
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => dict[documentUri]);

            var testNLHandler = new TestNLHandler { Expected = expectedExpr };
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory)
            {
                NL2FxImplementation = testNLHandler
            };

            List<Exception> exList = new List<Exception>();
            testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            testServer.OnDataReceived(FX2NlMessageJson(documentUri));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcFx2NLResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("123", response.Id);

            // result has expected concat with symbols. 
            Assert.Equal("Score > 3: True: sentence", response.Result.Explanation);
        }

        [Fact]
        public void ErrorIsLocalized()
        {
            var engine = new Engine(new PowerFxConfig());

            // ParseOptions locale
            var locale = CultureInfo.CreateSpecificCulture("fr-FR");

            engine.Config.AddFunction(new BehaviorFunction());

            _sendToClientData = new List<string>();
            _scopeFactory = new TestPowerFxScopeFactory(
                (string documentUri) => engine.CreateEditorScope(new ParserOptions() { Culture = locale }, GetFromUri(documentUri)));
            _testServer = new TestLanguageServer(_output, _sendToClientData.Add, _scopeFactory);

            List<Exception> exList = new List<Exception>();
            _testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didOpen",
                    @params = new DidOpenTextDocumentParams()
                    {
                        TextDocument = new TextDocumentItem()
                        {
                            Uri = "powerfx://app",
                            LanguageId = "powerfx",
                            Version = 1,
                            Text = "Bla."
                        }
                    }
                }));

            CheckBehaviorError(_sendToClientData[0], false, out var diags);

            // Checking if contains text in the correct locale
            Assert.Contains("Caractères inattendus.", diags.First().Message); // the value should be localized. Resx files have this localized.
            Assert.Empty(exList);
        }

        // Parse in Culture1, Errors in culture2
        [Fact]
        public void ParseAndErrorLocaleAreDifferent()
        {
            var engine = new Engine(new PowerFxConfig());

            var parseLocale = CultureInfo.CreateSpecificCulture("fr-FR");
            var errorLocale = CultureInfo.CreateSpecificCulture("es-ES");

            engine.Config.AddFunction(new BehaviorFunction());

            _sendToClientData = new List<string>();
            _scopeFactory = new TestPowerFxScopeFactory(
                (string documentUri) => new EditorContextScope(
                    (expr) => new CheckResult(engine)
                        .SetText(expr, new ParserOptions { Culture = parseLocale })
                        .SetBindingInfo()
                        .SetDefaultErrorCulture(errorLocale)));

            _testServer = new TestLanguageServer(_output, _sendToClientData.Add, _scopeFactory);
            List<Exception> exList = new List<Exception>();
            _testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            _testServer.OnDataReceived(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    method = "textDocument/didOpen",
                    @params = new DidOpenTextDocumentParams()
                    {
                        TextDocument = new TextDocumentItem()
                        {
                            Uri = "powerfx://app",
                            LanguageId = "powerfx",
                            Version = 1,
                            Text = "123,456 + foo"
                        }
                    }
                }));

            CheckBehaviorError(_sendToClientData[0], false, out var diags);

            // Checking if contains text in the correct locale
            // the value should be localized. Resx files have this localized.
            // If it's a different error message, then we may have a bug in the parser locale. 
            Assert.Contains("El nombre no es válido. No se reconoce \"foo\".", diags.First().Message);
            Assert.Empty(exList);
        }

        // Test showing how LSP can fully customize check result. 
        [Fact]
        public void CustomCheckResult()
        {
            _scopeFactory = new TestPowerFxScopeFactory(this.TestCreateEditorScope);
            _testServer = new TestLanguageServer(_output, _sendToClientData.Add, _scopeFactory);

            List<Exception> exList = new List<Exception>();
            _testServer.LogUnhandledExceptionHandler += (Exception ex) => exList.Add(ex);

            _testServer.OnDataReceived(
             JsonSerializer.Serialize(new
             {
                 jsonrpc = "2.0",
                 method = "textDocument/didOpen",
                 @params = new DidOpenTextDocumentParams()
                 {
                     TextDocument = new TextDocumentItem()
                     {
                         Uri = "powerfx://app",
                         LanguageId = "powerfx",
                         Version = 1,
                         Text = "12+34" // number, expecting string 
                     }
                 }
             }));

            CheckBehaviorError(_sendToClientData[0], false, out var diags);

            Assert.Contains("The type of this expression does not match the expected type 'Text'. Found type 'Decimal'.", diags.First().Message);
            Assert.Empty(exList);
        }

        [Fact]
        public void TestCorrectFullSemanticTokensAreReturnedWithExpressionInUri()
        {
            TestCorrectFullSemanticTokensAreReturned(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Max(1, 2, 3)"))
            });
        }

        [Fact]
        public void TestCorrectFullSemanticTokensAreReturnedWithExpressionNotInUri()
        {
            TestCorrectFullSemanticTokensAreReturned(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = "Max(1, 2, 3)"
            });
        }

        [Fact]
        public void TestCorrectFullSemanticTokensAreReturnedWithExpressionInBothUriAndTextDocument()
        {
            var expression = "Max(1, 2, 3)";
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("expression=Color.White")),
                Text = expression
            };
            TestCorrectFullSemanticTokensAreReturned(semanticTokenParams);
        }

        private void TestCorrectFullSemanticTokensAreReturned(SemanticTokensParams semanticTokensParams)
        {
            // Arrange
            var expression = GetExpression(semanticTokensParams);
            Assert.Equal("Max(1, 2, 3)", expression);
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokensParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            Assert.Single(decodedTokens.Where(tok => tok.TokenType == TokenType.Function));
            Assert.Equal(3, decodedTokens.Where(tok => tok.TokenType == TokenType.NumLit || tok.TokenType == TokenType.DecLit).Count());
        }

        [Theory]
        [InlineData("Create", TokenType.Function, TokenType.BoolLit)]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("[2, 3")]
        [InlineData("Create", TokenType.StrInterpStart, TokenType.BinaryOp, TokenType.NumLit, TokenType.DecLit, TokenType.Control)]
        [InlineData("[]")]
        [InlineData("1,2]")]
        [InlineData("[98]")]
        [InlineData("Create", TokenType.Lim)]
        [InlineData("Create", TokenType.BoolLit, TokenType.BinaryOp, TokenType.Function, TokenType.Lim)]
        [InlineData("Create", TokenType.Lim, TokenType.BinaryOp, TokenType.BoolLit)]
        [InlineData("   ")]
        [InlineData("NotPresent")]
        internal void TestCorrectFullSemanticTokensAreReturnedWithCertainTokenTypesSkipped(string tokenTypesToSkipParam, params TokenType[] tokenTypesToSkip)
        {
            // Arrange
            var expression = "1+-2;true;\"String Literal\";Sum(1,2);Max(1,2,3);$\"1 + 2 = {3}\";// This is Comment";
            var expectedTypes = new List<TokenType> { TokenType.DecLit, TokenType.BoolLit, TokenType.Comment, TokenType.Function, TokenType.StrInterpStart, TokenType.IslandEnd, TokenType.IslandStart, TokenType.StrLit, TokenType.StrInterpEnd, TokenType.Delimiter, TokenType.BinaryOp };
            if (tokenTypesToSkip.Length > 0)
            {
                expectedTypes = expectedTypes.Where(expectedType => !tokenTypesToSkip.Contains(expectedType)).ToList();
            }

            if (tokenTypesToSkipParam == "Create")
            {
                tokenTypesToSkipParam = JsonSerializer.Serialize(tokenTypesToSkip.Select(tokType => (int)tokType).ToList());
            }

            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri(tokenTypesToSkipParam == "NotPresent" ? string.Empty : "tokenTypesToSkip=" + tokenTypesToSkipParam)),
                Text = expression
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            var actualTypes = decodedTokens.Select(tok => tok.TokenType).Distinct().ToList();
            Assert.Equal(expectedTypes.OrderBy(type => type), actualTypes.OrderBy(type => type));
        }

        [Theory]
        [InlineData("Create", TokenType.Function, TokenType.BoolLit)]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("[2, 3")]
        [InlineData("Create", TokenType.StrInterpStart, TokenType.BinaryOp, TokenType.NumLit, TokenType.DecLit, TokenType.Control)]
        [InlineData("[]")]
        [InlineData("1,2]")]
        [InlineData("[98]")]
        [InlineData("Create", TokenType.Lim)]
        [InlineData("Create", TokenType.BoolLit, TokenType.BinaryOp, TokenType.Function, TokenType.Lim)]
        [InlineData("Create", TokenType.Lim, TokenType.BinaryOp, TokenType.BoolLit)]
        [InlineData("   ")]
        [InlineData("NotPresent")]
        internal void TestCorrectRangeSemanticTokensAreReturnedWithCertainTokenTypesSkipped(string tokenTypesToSkipParam, params TokenType[] tokenTypesToSkip)
        {
            // Arrange & Assert
            var expression = "1+1+1+1+1+1+1+1;Sqrt(1);1+-2;true;\n\"String Literal\";Sum(1,2);Max(1,2,3);$\"1 + 2 = {3}\";// This is Comment;//This is comment2;false";
            var range = SemanticTokensRelatedTestsHelper.CreateRange(1, 2, 3, 35);
            var expectedTypes = new List<TokenType> { TokenType.DecLit, TokenType.BoolLit, TokenType.Function, TokenType.StrLit, TokenType.Delimiter, TokenType.BinaryOp };
            if (tokenTypesToSkip.Length > 0)
            {
                expectedTypes = expectedTypes.Where(expectedType => !tokenTypesToSkip.Contains(expectedType)).ToList();
            }

            if (tokenTypesToSkipParam == "Create")
            {
                tokenTypesToSkipParam = JsonSerializer.Serialize(tokenTypesToSkip.Select(tokType => (int)tokType).ToList());
            }

            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(GetUri(tokenTypesToSkipParam == "NotPresent" ? string.Empty : "tokenTypesToSkip=" + tokenTypesToSkipParam)),
                Text = expression,
                Range = range
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData.FirstOrDefault(), payload.id);
            Assert.NotEmpty(response.Data);
            var decodedTokens = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression);
            var actualTypes = decodedTokens.Select(tok => tok.TokenType).Distinct().ToList();
            Assert.Equal(expectedTypes.OrderBy(type => type), actualTypes.OrderBy(type => type));
        }

        [Fact]
        public void TestErrorResponseReturnedWhenUriIsNullForFullSemanticTokensRequest()
        {
            // Arrange
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = new TextDocumentIdentifier() { Uri = null }
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            AssertErrorPayload(_sendToClientData.FirstOrDefault(), payload.id, JsonRpcHelper.ErrorCode.ParseError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestEmptyFullSemanticTokensResponseReturnedWhenExpressionIsInvalid(bool isNotNull)
        {
            // Arrange
            string expression = string.Empty;
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = isNotNull ? expression : null
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.Empty(response.Data);
        }

        [Fact]
        public void TestFullSemanticTokensResponseReturnedWithDefaultEOL()
        {
            // Arrange
            string expression = "Sum(\n1,1\n)";
            var semanticTokenParams = new SemanticTokensParams
            {
                TextDocument = GetTextDocument(),
                Text = expression 
            };
            var payload = GetFullDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.NotEmpty(response.Data);
            Assert.Equal(expression.Where(c => c == '\n').Count(), SemanticTokensRelatedTestsHelper.DetermineNumberOfLinesThatTokensAreSpreadAcross(response));
        }

        [Theory]
        [InlineData(1, 4, 1, 20, false, false)]
        [InlineData(1, 6, 1, 20, true, false)]
        [InlineData(1, 1, 1, 12, false, true)]
        [InlineData(1, 5, 1, 24, true, false)]
        [InlineData(1, 5, 1, 15, true, true)]
        [InlineData(1, 9, 1, 14, true, true)]
        [InlineData(1, 1, 3, 34, false, false)]
        [InlineData(1, 3, 2, 12, false, true)]
        [InlineData(2, 11, 3, 3, true, true)]
        [InlineData(1, 24, 3, 17, false, false)]
        public void TestCorrectRangeSemanticTokensAreReturned(int startLine, int startLineCol, int endLine, int endLineCol, bool tokenDoesNotAlignOnLeft, bool tokenDoesNotAlignOnRight)
        {
            // Arrange
            var expression = "If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone\nNumber\"),Notify(\"Valid Phone No\"))";
            var eol = "\n";
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = expression,
                Range = SemanticTokensRelatedTestsHelper.CreateRange(startLine, endLine, startLineCol, endLineCol)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData.FirstOrDefault(), payload.id);
            var (startIndex, endIndex) = semanticTokenParams.Range.ConvertRangeToPositions(expression, eol);
            var decodedResponse = SemanticTokensRelatedTestsHelper.DecodeEncodedSemanticTokensPartially(response, expression, eol);

            var leftMostTok = decodedResponse.Min(tok => tok.StartIndex);
            var rightMostTok = decodedResponse.Max(tok => tok.EndIndex);

            Assert.All(decodedResponse, (tok) => Assert.False(tok.EndIndex <= leftMostTok || tok.StartIndex >= rightMostTok));
            if (tokenDoesNotAlignOnLeft)
            {
                Assert.True(leftMostTok < startIndex);
            }
            else
            {
                Assert.True(leftMostTok >= startIndex);
            }

            if (tokenDoesNotAlignOnRight)
            {
                Assert.True(rightMostTok > endIndex);
            }
            else
            {
                Assert.True(rightMostTok <= endIndex);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestEmptyRangeSemanticTokensResponseReturnedWhenExpressionIsInvalid(bool isNotNull)
        {
            // Arrange
            string expression = string.Empty;
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = isNotNull ? expression : null,
                Range = SemanticTokensRelatedTestsHelper.CreateRange(1, 1, 1, 4)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.Empty(response.Data);
        }

        [Fact]
        public void TestErrorResponseReturnedWhenUriIsNullForRangeSemanticTokensRequest()
        {
            // Arrange
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = new TextDocumentIdentifier() { Uri = null },
                Range = SemanticTokensRelatedTestsHelper.CreateRange(1, 1, 1, 4)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            AssertErrorPayload(_sendToClientData.FirstOrDefault(), payload.id, JsonRpcHelper.ErrorCode.ParseError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestEmptyRangeSemanticTokensResponseReturnedWhenRangeIsNullOrInvalid(bool isNull)
        {
            // Arrange
            var expression = "If(Len(Phone_Number) < 10,\nNotify(\"Invalid Phone\nNumber\"),Notify(\"Valid Phone No\"))";
            var semanticTokenParams = new SemanticTokensRangeParams
            {
                TextDocument = GetTextDocument(),
                Text = expression,
                Range = isNull ? null : SemanticTokensRelatedTestsHelper.CreateRange(expression.Length + 2, 2, 1, 2)
            };
            var payload = GetRangeDocumentSemanticTokensRequestPayload(semanticTokenParams);

            // Act
            _testServer.OnDataReceived(payload.payload);

            // Assert
            var response = AssertAndGetSemanticTokensResponse(_sendToClientData?.FirstOrDefault(), payload.id);
            Assert.Empty(response.Data);
        }

        [Theory]
        [InlineData("9 + 9", 0)]
        [InlineData("Max(10, 20, 30)", 0)]
        [InlineData("Color.AliceBlue", 0)]
        [InlineData("9 + 9\n Max(1, 3, 19); \n Color.AliceBlue", 0)]
        [InlineData("Label2.Text", 1)]
        [InlineData("NestedLabel1", 1)]
        [InlineData("Label2.Text;\nNestedLabel1", 2)]
        public void TestPublishControlTokensNotification(string expression, int expectedNumberOfControlTokens)
        {
            // Arrange
            var engine = new Engine(new PowerFxConfig());
            var checkResult = SemanticTokenTestHelper.GetCheckResultWithControlSymbols(expression);
            var scopeFactory = new TestPowerFxScopeFactory(
                (string documentUri) => new EditorContextScope(
                    (expr) => checkResult));
            var testServer = new TestLanguageServer(_output, _sendToClientData.Add, scopeFactory);

            var payload = GetFullDocumentSemanticTokensRequestPayload(new SemanticTokensParams
            {
                TextDocument = GetTextDocument(GetUri("&version=someVersionId")),
                Text = expression
            });

            // Act
            testServer.OnDataReceived(payload.payload);
            if (expectedNumberOfControlTokens > 0)
            {
                var notification = JsonSerializer.Deserialize<JsonRpcPublishControlTokensNotification>(_sendToClientData[1], _jsonSerializerOptions);

                // Assert
                Assert.Equal("2.0", notification.Jsonrpc);
                Assert.Equal("$/publishControlTokens", notification.Method);
                Assert.Equal("someVersionId", notification.Params.Version);

                var controlTokenList = notification.Params.Data;
                Assert.Equal(expectedNumberOfControlTokens, controlTokenList.Count());
                foreach (var controlToken in controlTokenList)
                {
                    Assert.Equal(typeof(ControlToken), controlToken.GetType());
                }
            }
            else
            {
                // No control tokens, then no control token notification created. 
                // Here the only data is the semantic token data.
                Assert.Single(_sendToClientData);
            }            
        }

        private static SemanticTokensResponse AssertAndGetSemanticTokensResponse(string response, string id)
        {
            var tokensResponse = AssertAndGetResponsePayload<SemanticTokensResponse>(response, id);
            Assert.NotNull(tokensResponse);
            Assert.NotNull(tokensResponse.Data);
            return tokensResponse;
        }

        private static void AssertErrorPayload(string response, string id, JsonRpcHelper.ErrorCode expectedCode)
        {
            Assert.NotNull(response);
            var deserializedResponse = JsonDocument.Parse(response);
            var root = deserializedResponse.RootElement;
            Assert.True(root.TryGetProperty("id", out var responseId));
            Assert.Equal(id, responseId.GetString());
            Assert.True(root.TryGetProperty("error", out var errElement));
            Assert.True(errElement.TryGetProperty("code", out var codeElement));
            var code = (JsonRpcHelper.ErrorCode)codeElement.GetInt32();
            Assert.Equal(expectedCode, code);
        }

        private static T AssertAndGetResponsePayload<T>(string response, string id)
        {
            Assert.NotNull(response);
            var deserializedResponse = JsonDocument.Parse(response);
            var root = deserializedResponse.RootElement;
            root.TryGetProperty("id", out var responseId);
            Assert.Equal(id, responseId.GetString());
            root.TryGetProperty("result", out var resultElement);
            var paramsObj = JsonSerializer.Deserialize<T>(resultElement.GetRawText(), _jsonSerializerOptions);
            return paramsObj;
        }

        private static (string payload, string id) GetRangeDocumentSemanticTokensRequestPayload(SemanticTokensRangeParams semanticTokenRangeParams, string id = null)
        {
            return GetRequestPayload(semanticTokenRangeParams, TextDocumentNames.RangeDocumentSemanticTokens, id);
        }

        private static (string payload, string id) GetFullDocumentSemanticTokensRequestPayload(SemanticTokensParams semanticTokenParams, string id = null)
        {
            return GetRequestPayload(semanticTokenParams, TextDocumentNames.FullDocumentSemanticTokens, id);
        }

        private static string GetExpression(LanguageServerRequestBaseParams requestParams)
        {
            if (requestParams?.Text != null)
            {
                return requestParams.Text;
            }

            var uri = new Uri(requestParams.TextDocument.Uri);
            return HttpUtility.ParseQueryString(uri.Query).Get("expression");
        }

        private static string GetUri(string queryParams = null)
        {
            var uriBuilder = new UriBuilder("powerfx://app")
            {
                Query = queryParams ?? string.Empty
            };
            return uriBuilder.Uri.AbsoluteUri;
        }

        private static (string payload, string id) GetRequestPayload<T>(T paramsObj, string method, string id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var payload = JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                method,
                @params = paramsObj
            }, _jsonSerializerOptions);
            return (payload, id);
        }

        private static TextDocumentIdentifier GetTextDocument(string uri = null)
        {
            return new TextDocumentIdentifier() { Uri = uri ?? GetUri() };
        }

        [Fact]
        public async Task TestExpectedReturnValueForEmptyExpression()
        {
            var scope = TestCreateEditorScope(string.Empty);

            var check = scope.Check(string.Empty);

            Assert.True(check.IsSuccess);

            var run = check.GetEvaluator();

            var result = await run.EvalAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Null(result.ToObject());
        }

        [Fact]
        public void SerializeUnsupportedType()
        {
            var unsupportedType = FormulaType.Build(DType.Polymorphic);
            var serialized = JsonSerializer.Serialize<FormulaType>(unsupportedType, _jsonSerializerOptions);
            Assert.Equal("{\"Type\":\"Unsupported\"}", serialized);

            Assert.Throws<NotImplementedException>(() => JsonSerializer.Deserialize<FormulaType>(serialized, _jsonSerializerOptions));
        }

        private EditorContextScope TestCreateEditorScope(string documentUri)
        {
            var engine = new Engine();

            return new EditorContextScope((expression) => new CheckResult(engine)
                .SetText(expression)
                .SetBindingInfo()
                .SetExpectedReturnValue(FormulaType.String));
        }
    }
}
