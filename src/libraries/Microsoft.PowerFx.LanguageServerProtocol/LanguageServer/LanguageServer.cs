// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Web;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// PowerFx Language server implementation
    ///
    /// LanguageServer can receive request/notification payload from client, it can also send request/notification to client.
    ///
    /// LanguageServer is hosted inside WebSocket or HTTP/HTTPS service
    ///   * For WebSocket, OnDataReceived() is for incoming traffic, SendToClient() is for outgoing traffic
    ///   * For HTTP/HTTPS, OnDataReceived() is for HTTP/HTTPS request, SendToClient() is queued up in next HTTP/HTTPS response.
    /// </summary>
    public class LanguageServer
    {
        private const char EOL = '\n';

        public delegate void SendToClient(string data);

        private readonly SendToClient _sendToClient;

        private readonly IPowerFxScopeFactory _scopeFactory;

        public delegate void NotifyDidChange(DidChangeTextDocumentParams didChangeParams);

        public event NotifyDidChange OnDidChange;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        public LanguageServer(SendToClient sendToClient, IPowerFxScopeFactory scopeFactory)
        {
            Contracts.AssertValue(sendToClient);
            Contracts.AssertValue(scopeFactory);

            _sendToClient = sendToClient;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Received request/notification payload from client.
        /// </summary>
        public void OnDataReceived(string jsonRpcPayload)
        {
            Contracts.AssertValue(jsonRpcPayload);

            string id = null;
            try
            {
                using (var doc = JsonDocument.Parse(jsonRpcPayload))
                {
                    var element = doc.RootElement;
                    if (element.TryGetProperty("id", out var idElement))
                    {
                        id = idElement.GetString();
                    }

                    if (!element.TryGetProperty("method", out var methodElement))
                    {
                        _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                        return;
                    }

                    if (!element.TryGetProperty("params", out var paramsElement))
                    {
                        _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                        return;
                    }

                    var method = methodElement.GetString();
                    var paramsJson = paramsElement.GetRawText();
                    switch (method)
                    {
                        case TextDocumentNames.DidOpen:
                            HandleDidOpenNotification(paramsJson);
                            break;
                        case TextDocumentNames.DidChange:
                            HandleDidChangeNotification(paramsJson);
                            break;
                        case TextDocumentNames.Completion:
                            HandleCompletionRequest(id, paramsJson);
                            break;
                        case TextDocumentNames.SignatureHelp:
                            HandleSignatureHelpRequest(id, paramsJson);
                            break;
                        case CustomProtocolNames.InitialFixup:
                            HandleInitialFixupRequest(id, paramsJson);
                            break;
                        case TextDocumentNames.CodeAction:
                            HandleCodeActionRequest(id, paramsJson);
                            break;
                        default:
                            _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.MethodNotFound));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, ex.Message));
                return;
            }
        }

        private void HandleDidOpenNotification(string paramsJson)
        {
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out DidOpenTextDocumentParams didOpenParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(null, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = didOpenParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var expression = didOpenParams.TextDocument.Text;
            var result = scope.Check(expression);

            PublishDiagnosticsNotification(documentUri, expression, result.Errors);

            PublishTokens(documentUri, result);

            PublishExpressionType(documentUri, result);
        }

        private void HandleDidChangeNotification(string paramsJson)
        {
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out DidChangeTextDocumentParams didChangeParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(null, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            if (didChangeParams.ContentChanges.Length != 1)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(null, JsonRpcHelper.ErrorCode.InvalidParams));
                return;
            }

            OnDidChange?.Invoke(didChangeParams);

            var documentUri = didChangeParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var expression = didChangeParams.ContentChanges[0].Text;
            var result = scope.Check(expression);

            PublishDiagnosticsNotification(documentUri, expression, result.Errors);

            PublishTokens(documentUri, result);

            PublishExpressionType(documentUri, result);
        }

        private void HandleCompletionRequest(string id, string paramsJson)
        {
            if (id == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out CompletionParams completionParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = completionParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var uri = new Uri(documentUri);
            var expression = HttpUtility.ParseQueryString(uri.Query).Get("expression");
            if (expression == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidParams));
                return;
            }

            var cursorPosition = GetPosition(expression, completionParams.Position.Line, completionParams.Position.Character);
            var result = scope.Suggest(expression, cursorPosition);

            var items = new List<CompletionItem>();

            foreach (var item in result.Suggestions)
            {
                items.Add(new CompletionItem()
                {
                    Label = item.DisplayText.Text,
                    Detail = item.FunctionParameterDescription,
                    Documentation = item.Definition,
                    Kind = GetCompletionItemKind(item.Kind)
                });
            }

            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new
            {
                items,
                isIncomplete = false
            }));
        }

        private void HandleSignatureHelpRequest(string id, string paramsJson)
        {
            if (id == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out SignatureHelpParams signatureHelpParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = signatureHelpParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var uri = new Uri(documentUri);
            var expression = HttpUtility.ParseQueryString(uri.Query).Get("expression");
            if (expression == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidParams));
                return;
            }

            var cursorPosition = GetPosition(expression, signatureHelpParams.Position.Line, signatureHelpParams.Position.Character);
            var result = scope.Suggest(expression, cursorPosition);

            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, result.SignatureHelp));
        }

        private void HandleInitialFixupRequest(string id, string paramsJson)
        {
            if (id == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out InitialFixupParams initialFixupParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = initialFixupParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var expression = initialFixupParams.TextDocument.Text;
            if (scope is IPowerFxScopeDisplayName scopeDisplayName)
            {
                expression = scopeDisplayName.TranslateToDisplayName(expression);
            }

            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new TextDocumentItem()
            {
                Uri = documentUri,
                Text = expression
            }));
        }

        private void HandleCodeActionRequest(string id, string paramsJson)
        {
            if (id == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out CodeActionParams codeActionParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = codeActionParams.TextDocument.Uri;

            var uri = new Uri(documentUri);
            var expression = HttpUtility.ParseQueryString(uri.Query).Get("expression");
            if (expression == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidParams));
                return;
            }

            var codeActions = new Dictionary<string, CodeAction[]>();
            foreach (var codeActionKind in codeActionParams.Context.Only)
            {
                switch (codeActionKind)
                {
                    case CodeActionKind.QuickFix:

                        var scope = _scopeFactory.GetOrCreateInstance(documentUri);
                        var scopeQuickFix = scope as IPowerFxScopeQuickFix;

                        if (scopeQuickFix != null)
                        {
                            var result = scopeQuickFix.Suggest(expression);

                            var items = new List<CodeAction>();

                            foreach (var item in result)
                            {
                                var range = item.Range ?? codeActionParams.Range;
                                items.Add(new CodeAction()
                                {
                                    Title = item.Title,
                                    Kind = codeActionKind,
                                    Edit = new WorkspaceEdit
                                    {
                                        Changes = new Dictionary<string, TextEdit[]> { { documentUri, new[] { new TextEdit { Range = range, NewText = item.Text } } } }
                                    }
                                });
                            }

                            codeActions.Add(codeActionKind, items.ToArray());
                        }

                        break;
                    default:
                        // No action.
                        return;
                }
            }

            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, codeActions));
        }

        private CompletionItemKind GetCompletionItemKind(SuggestionKind kind)
        {
            switch (kind)
            {
                case SuggestionKind.Function:
                    return CompletionItemKind.Method;
                case SuggestionKind.KeyWord:
                    return CompletionItemKind.Keyword;
                case SuggestionKind.Global:
                    return CompletionItemKind.Variable;
                case SuggestionKind.Field:
                    return CompletionItemKind.Field;
                case SuggestionKind.Alias:
                    return CompletionItemKind.Variable;
                case SuggestionKind.Enum:
                    return CompletionItemKind.Enum;
                case SuggestionKind.BinaryOperator:
                    return CompletionItemKind.Operator;
                case SuggestionKind.Local:
                    return CompletionItemKind.Variable;
                case SuggestionKind.ServiceFunctionOption:
                    return CompletionItemKind.Method;
                case SuggestionKind.Service:
                    return CompletionItemKind.Module;
                case SuggestionKind.ScopeVariable:
                    return CompletionItemKind.Variable;
                default:
                    return CompletionItemKind.Text;
            }
        }

        /// <summary>
        /// PowerFx classifies diagnostics by <see cref="DocumentErrorSeverity"/>, LSP classifies them by
        /// <see cref="DiagnosticSeverity"/>. This method maps the former to the latter.
        /// </summary>
        /// <param name="severity">
        /// <see cref="DocumentErrorSeverity"/> which will be mapped to the LSP eequivalent.
        /// </param>
        /// <returns>
        /// <see cref="DiagnosticSeverity"/> equivalent to <see cref="DocumentErrorSeverity"/>.
        /// </returns>
        private DiagnosticSeverity DocumentSeverityToDiagnosticSeverityMap(DocumentErrorSeverity severity) => severity switch
        {
            DocumentErrorSeverity.Critical => DiagnosticSeverity.Error,
            DocumentErrorSeverity.Severe => DiagnosticSeverity.Error,
            DocumentErrorSeverity.Moderate => DiagnosticSeverity.Error,
            DocumentErrorSeverity.Warning => DiagnosticSeverity.Warning,
            DocumentErrorSeverity.Suggestion => DiagnosticSeverity.Hint,
            DocumentErrorSeverity.Verbose => DiagnosticSeverity.Information,
            _ => DiagnosticSeverity.Information
        };

        private void PublishDiagnosticsNotification(string uri, string expression, ExpressionError[] errors)
        {
            Contracts.AssertNonEmpty(uri);
            Contracts.AssertValue(expression);

            var diagnostics = new List<Diagnostic>();
            if (errors != null)
            {
                foreach (var item in errors)
                {
                    var span = item.Span;
                    var startCode = expression.Substring(0, span.Min);
                    var code = expression.Substring(span.Min, span.Lim - span.Min);
                    var startLine = startCode.Split(EOL).Length;
                    var startChar = GetCharPosition(expression, span.Min);
                    var endLine = startLine + code.Split(EOL).Length - 1;
                    var endChar = GetCharPosition(expression, span.Lim) - 1;

                    diagnostics.Add(new Diagnostic()
                    {
                        Range = new Protocol.Range()
                        {
                            Start = new Position()
                            {
                                Character = startChar,
                                Line = startLine
                            },
                            End = new Position()
                            {
                                Character = endChar,
                                Line = endLine
                            }
                        },
                        Message = item.Message,
                        Severity = DocumentSeverityToDiagnosticSeverityMap(item.Severity)
                    });
                }
            }

            // Send PublishDiagnostics notification
            _sendToClient(JsonRpcHelper.CreateNotification(
                TextDocumentNames.PublishDiagnostics,
                new PublishDiagnosticsParams()
                {
                    Uri = uri,
                    Diagnostics = diagnostics.ToArray()
                }));
        }

        private void PublishTokens(string documentUri, CheckResult result)
        {
            var uri = new Uri(documentUri);
            var nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            if (!uint.TryParse(nameValueCollection.Get("getTokensFlags"), out var flags))
            {
                return;
            }

            var tokens = result.GetTokens((GetTokensFlags)flags);
            if (tokens == null || tokens.Count == 0)
            {
                return;
            }

            // Send PublishTokens notification
            _sendToClient(JsonRpcHelper.CreateNotification(
                CustomProtocolNames.PublishTokens,
                new PublishTokensParams()
                {
                    Uri = documentUri,
                    Tokens = tokens
                }));
        }

        private void PublishExpressionType(string documentUri, CheckResult result)
        {
            var uri = new Uri(documentUri);
            var nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            if (!bool.TryParse(nameValueCollection.Get("getExpressionType"), out var enabled) || !enabled)
            {
                return;
            }

            _sendToClient(JsonRpcHelper.CreateNotification(
                CustomProtocolNames.PublishExpressionType,
                new PublishExpressionTypeParams()
                {
                    Uri = documentUri,
                    Type = result.ReturnType
                }));
        }

        private bool TryParseParams<T>(string json, out T result)
        {
            Contracts.AssertNonEmpty(json);

            try
            {
                result = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Get the charactor position (starts with 1) from its line.
        /// e.g. "123\n1{2}3" ==> 2 ({x} is the input char at position)
        ///      "12{\n}123" ==> 3 ('\n' belongs to the previous line "12\n", the last char is '2' with index of 3).
        /// </summary>
        /// <param name="expression">The expression content.</param>
        /// <param name="position">The charactor position (starts with 0).</param>
        /// <returns>The charactor position (starts with 1) from its line.</returns>
        protected int GetCharPosition(string expression, int position)
        {
            Contracts.AssertValue(expression);
            Contracts.Assert(position >= 0);

            var column = (position < expression.Length && expression[position] == EOL) ? 0 : 1;
            position--;
            while (position >= 0 && expression[position] != EOL)
            {
                column++;
                position--;
            }

            return column;
        }

        /// <summary>
        /// Get the position offset (starts with 0) in Expression from line/character (starts with 0)
        /// e.g. "123", line:0, char:1 => 1.
        /// </summary>
        protected int GetPosition(string expression, int line, int character)
        {
            Contracts.AssertValue(expression);
            Contracts.Assert(line >= 0);
            Contracts.Assert(character >= 0);

            var position = 0;
            var currentLine = 0;
            var currentCharacter = 0;
            while (position < expression.Length)
            {
                if (line == currentLine && character == currentCharacter)
                {
                    return position;
                }

                if (expression[position] == EOL)
                {
                    currentLine++;
                    currentCharacter = 0;
                }
                else
                {
                    currentCharacter++;
                }

                position++;
            }

            return position;
        }
    }
}
