// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    public class LanguageServerTests : PowerFxTest
    {       
        protected static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new FormulaTypeJsonConverter() }
        };

        protected List<string> _sendToClientData;
        protected TestPowerFxScopeFactory _scopeFactory;
        protected TestLanguageServer _testServer;

        public LanguageServerTests()
        {
            // Create an Engine() that has all the builtin symbols by default. 
            // Note that interpreter has fewer symbols. 
            var engine = new Engine(new PowerFxConfig());

            _sendToClientData = new List<string>();
            _scopeFactory = new TestPowerFxScopeFactory((string documentUri) => RecalcEngineScope.FromUri(engine, documentUri));
            _testServer = new TestLanguageServer(_sendToClientData.Add, _scopeFactory);
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
            Assert.Equal(list[0].Message, errorResponse.Error.Message);
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
            var testServer = new TestLanguageServer(_sendToClientData.Add, scopeFactory);

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
            Assert.Equal(list[0].Message, errorResponse.Error.Message);
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
        }

        [Fact]
        public void TestDidChange()
        {
            // test good formula
            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
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
                        new TextDocumentContentChangeEvent() { Text = "A+CountRows(B)" }
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

            for (var i = 0; i < expectedDiagnostics.Length; i++)
            {
                var expectedDiagnostic = expectedDiagnostics[i];
                var actualDiagnostic = notification.Params.Diagnostics[i];
                Assert.Equal(expectedDiagnostic.Message, actualDiagnostic.Message);
            }
        }

        [Theory]
        [InlineData("A+CountRows(B)", "{\"A\":1,\"B\":[1,2,3]}")]
        public void TestDidOpenValidFormula(string formula, string context = null)
        {
            var uri = $"powerfx://app{(context != null ? "powerfx://app?context=" + context : string.Empty)}";
            TestPublishDiagnostics(uri, "textDocument/didOpen", formula, new Diagnostic[0]);
        }

        [Theory]
        [InlineData("AA", null, "Name isn't valid. 'AA' isn't recognized.")]
        [InlineData("1+CountRowss", null, "Name isn't valid. 'CountRowss' isn't recognized.")]
        [InlineData("CountRows(2)", null, "The function 'CountRows' has some invalid arguments.", "Invalid argument type (Number). Expecting a Table value instead.")]
        public void TestDidOpenErroneousFormula(string formula, string context, params string[] expectedErrors)
        {
            var expectedDiagnostics = expectedErrors.Select(error => new Diagnostic()
            {
                Message = error,
                Severity = DiagnosticSeverity.Error
            }).ToArray();
            TestPublishDiagnostics("powerfx://app", "textDocument/didOpen", formula, expectedDiagnostics);
        }

        [Fact]
        public void TestDidOpenSeverityFormula()
        {
            var formula = "Count([\"test\"])";
            var expectedDiagnostics = new[]
            {
                new Diagnostic()
                {
                    Message = "Invalid schema, expected a column of numeric values for 'Value'.",
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

        [Fact]
        public void TestCompletion()
        {
            // test good formula
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/completion",
                @params = new CompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test?expression=Color.AliceBl&context={}"
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = 13
                    },
                    Context = new CompletionContext()
                }
            }));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("123", response.Id);
            var foundItems = response.Result.Items.Where(item => item.Label == "AliceBlue");
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");

            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/completion",
                @params = new CompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test?expression=Color.&context={\"A\":\"ABC\",\"B\":{\"Inner\":123}}"
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = 7
                    },
                    Context = new CompletionContext()
                }
            }));
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("123", response.Id);
            foundItems = response.Result.Items.Where(item => item.Label == "AliceBlue");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            Assert.True(Enumerable.Count(foundItems) == 1, "AliceBlue should be found from suggestion result");

            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/completion",
                @params = new CompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test?expression={a:{},b:{},c:{}}."
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = 17
                    },
                    Context = new CompletionContext()
                }
            }));
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcCompletionResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("123", response.Id);
            foundItems = response.Result.Items.Where(item => item.Label == "a");
            Assert.True(Enumerable.Count(foundItems) == 1, "'a' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            foundItems = response.Result.Items.Where(item => item.Label == "b");
            Assert.True(Enumerable.Count(foundItems) == 1, "'b' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);
            foundItems = response.Result.Items.Where(item => item.Label == "c");
            Assert.True(Enumerable.Count(foundItems) == 1, "'c' should be found from suggestion result");
            Assert.Equal(CompletionItemKind.Variable, foundItems.First().Kind);

            // missing 'expression' in documentUri
            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/completion",
                @params = new CompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test"
                    },
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
        }

        [Fact]
        public void TestCodeAction()
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockSqlEngine());
            var testServer = new TestLanguageServer(_sendToClientData.Add, scopeFactory);
            var documentUri = "powerfx://test?expression=IsBlank(&context={\"A\":1,\"B\":[1,2,3]}";

            testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "testDocument1",
                method = "textDocument/codeAction",
                @params = new CodeActionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = documentUri
                    },
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
        }

        [Fact]
        public void TestSignatureHelp()
        {
            // test good formula
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/signatureHelp",
                @params = new SignatureHelpParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test?expression=Power(&context={}"
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = 6
                    },
                    Context = new SignatureHelpContext()
                    {
                        TriggerKind = SignatureHelpTriggerKind.TriggerCharacter,
                        TriggerCharacter = "("
                    }
                }
            }));
            Assert.Single(_sendToClientData);
            var response = JsonSerializer.Deserialize<JsonRpcSignatureHelpResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("123", response.Id);
            Assert.Equal(0U, response.Result.ActiveSignature);
            Assert.Equal(0U, response.Result.ActiveParameter);
            var foundItems = response.Result.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) == 1, "Power should be found from signatures result");
            Assert.Equal(0U, foundItems.First().ActiveParameter);
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/signatureHelp",
                @params = new SignatureHelpParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test?expression=Power(2,&context={}"
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = 8
                    },
                    Context = new SignatureHelpContext()
                    {
                        TriggerKind = SignatureHelpTriggerKind.TriggerCharacter,
                        TriggerCharacter = ","
                    }
                }
            }));
            Assert.Single(_sendToClientData);
            response = JsonSerializer.Deserialize<JsonRpcSignatureHelpResponse>(_sendToClientData[0], _jsonSerializerOptions);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("123", response.Id);
            Assert.Equal(0U, response.Result.ActiveSignature);
            Assert.Equal(1U, response.Result.ActiveParameter);
            foundItems = response.Result.Signatures.Where(item => item.Label.StartsWith("Power"));
            Assert.True(Enumerable.Count(foundItems) == 1, "Power should be found from signatures result");
            Assert.Equal(0U, foundItems.First().ActiveParameter);
            Assert.Equal(2, foundItems.First().Parameters.Length);
            Assert.Equal("base", foundItems.First().Parameters[0].Label);
            Assert.Equal("exponent", foundItems.First().Parameters[1].Label);

            // missing 'expression' in documentUri
            _sendToClientData.Clear();
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = "123",
                method = "textDocument/signatureHelp",
                @params = new CompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = "powerfx://test"
                    },
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
        }

        [Fact]
        public void TestPublishTokens()
        {
            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=1";
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
                        Text = "A+CountRows(B)"
                    }
                }
            }));
            Assert.Equal(2, _sendToClientData.Count);
            var response = JsonSerializer.Deserialize<JsonRpcPublishTokensNotification>(_sendToClientData[1], _jsonSerializerOptions);
            Assert.Equal("$/publishTokens", response.Method);
            Assert.Equal(documentUri, response.Params.Uri);
            Assert.Equal(3, response.Params.Tokens.Count);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["A"]);
            Assert.Equal(TokenResultType.Variable, response.Params.Tokens["B"]);
            Assert.Equal(TokenResultType.Function, response.Params.Tokens["CountRows"]);

            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            _sendToClientData.Clear();
            documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=2";
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
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
                        new TextDocumentContentChangeEvent() { Text = "A+CountRows(B)" }
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

            // getTokensFlags = 0x0 (none), 0x1 (tokens inside expression), 0x2 (all functions)
            _sendToClientData.Clear();
            documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}&getTokensFlags=3";
            _testServer.OnDataReceived(JsonSerializer.Serialize(new
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
                        new TextDocumentContentChangeEvent() { Text = "A+CountRows(B)" }
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
        }

        [Theory]
        [InlineData("{\"A\": 1 }", "A+2", typeof(NumberType))]
        [InlineData("{}", "\"hi\"", typeof(StringType))]
        [InlineData("{}", "", typeof(BlankType))]
        [InlineData("{}", "{ A: 1 }", typeof(RecordType))]
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
        }

        [Theory]
        [InlineData("{}", "{ A: 1 }", @"{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Number""}}}")]
        [InlineData("{}", "[1, 2]", @"{""Type"":""Table"",""Fields"":{""Value"":{""Type"":""Number""}}}")]
        [InlineData("{}", "[{ A: 1 }, { B: true }]", @"{""Type"":""Table"",""Fields"":{""Value"":{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Number""},""B"":{""Type"":""Boolean""}}}}}")]
        [InlineData("{}", "{A: 1, B: { C: { D: \"Qwerty\" }, E: true } }", @"{""Type"":""Record"",""Fields"":{""A"":{""Type"":""Number""},""B"":{""Type"":""Record"",""Fields"":{""C"":{""Type"":""Record"",""Fields"":{""D"":{""Type"":""String""}}},""E"":{""Type"":""Boolean""}}}}}")]
        [InlineData("{}", "{ type: 123 }", @"{""Type"":""Record"",""Fields"":{""type"":{""Type"":""Number""}}}")]
        public void TestPublishExpressionType_AggregateShapes(string context, string expression, string expectedTypeJson)
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
            Assert.Equal(expectedTypeJson, JsonSerializer.Serialize(response.Params.Type, _jsonSerializerOptions));
        }

        [Fact]
        public void TestInitialFixup()
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockSqlEngine());
            var testServer = new TestLanguageServer(_sendToClientData.Add, scopeFactory);
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
        }
    }
}
