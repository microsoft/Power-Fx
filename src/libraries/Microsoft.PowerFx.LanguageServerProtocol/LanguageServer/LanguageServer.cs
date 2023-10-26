// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;
using Microsoft.PowerFx.Syntax;

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
        private readonly Action<string> _logger;

        public delegate void NotifyDidChange(DidChangeTextDocumentParams didChangeParams);

        public event NotifyDidChange OnDidChange;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        public delegate void OnLogUnhandledExceptionHandler(Exception e);

        /// <summary>
        /// Callback for host to get notified of unhandled exceptions that are happening asynchronously.
        /// This should be used for logging purposes. 
        /// </summary>
        public event OnLogUnhandledExceptionHandler LogUnhandledExceptionHandler;

        /// <summary>
        /// If set, provides the handler for $/nlSuggestion message.
        /// </summary>
        public NLHandler NL2FxImplementation { get; set; }

        public LanguageServer(SendToClient sendToClient, IPowerFxScopeFactory scopeFactory, Action<string> logger = null)
        {
            Contracts.AssertValue(sendToClient);
            Contracts.AssertValue(scopeFactory);

            _sendToClient = sendToClient;
            _scopeFactory = scopeFactory;

            _logger = logger;
        }

        /// <summary>
        /// Received request/notification payload from client.
        /// </summary>
        public void OnDataReceived(string jsonRpcPayload)
        {
            Contracts.AssertValue(jsonRpcPayload);

            // Only log when logger is provided and not null
            // Creating log string is expensive here, especially for the completion requests
            // Only create log strings when logger is not null
            _logger?.Invoke($"[PFX] OnDataReceived Received: {jsonRpcPayload ?? "<null>"}");

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
                        _logger?.Invoke($"[PFX] OnDataReceived CreateErrorResult InvalidRequest (method not found)");
                        _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                        return;
                    }

                    if (!element.TryGetProperty("params", out var paramsElement))
                    {
                        _logger?.Invoke($"[PFX] OnDataReceived CreateErrorResult InvalidRequest (params not found)");
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
                        case CustomProtocolNames.CommandExecuted:
                            HandleCommandExecutedRequest(id, paramsJson);
                            break;

                        case CustomProtocolNames.GetCapabilities:
                            new GetCapabilitiesHandler(this).Handle(id, paramsJson);
                            break;
                        case CustomProtocolNames.NL2FX:
                            new NL2FxHandler(this).Handle(id, paramsJson);
                            break;
                        case CustomProtocolNames.FX2NL:
                            new Fx2NLHandler(this).Handle(id, paramsJson);
                            break;

                        case TextDocumentNames.FullDocumentSemanticTokens:
                            HandleFullDocumentSemanticTokens(id, paramsJson);
                            break;
                        case TextDocumentNames.RangeDocumentSemanticTokens:
                            HandleRangeDocumentSemanticTokens(id, paramsJson);
                            break;
                        default:
                            _logger?.Invoke($"[PFX] OnDataReceived CreateErrorResult InvalidRequest (unknown method)");
                            _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.MethodNotFound));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[PFX] OnDataReceived Exception: {ex.GetDetailedExceptionMessage()}");

                LogUnhandledExceptionHandler?.Invoke(ex);

                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InternalError, ex.GetDetailedExceptionMessage()));
                return;
            }
        }

        private void HandleCommandExecutedRequest(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleCommandExecutedRequest: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

            if (id == null)
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out CommandExecutedParams commandExecutedParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            if (string.IsNullOrWhiteSpace(commandExecutedParams.Argument))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.PropertyValueRequired, $"{nameof(CommandExecutedParams.Argument)} is null or empty."));
                return;
            }

            switch (commandExecutedParams.Command)
            {
                case CommandName.CodeActionApplied:
                    var codeActionResult = JsonRpcHelper.Deserialize<CodeAction>(commandExecutedParams.Argument);
                    if (codeActionResult.ActionResultContext == null)
                    {
                        _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.PropertyValueRequired, $"{nameof(CodeAction.ActionResultContext)} is null or empty."));
                        return;
                    }

                    var scope = _scopeFactory.GetOrCreateInstance(commandExecutedParams.TextDocument.Uri);
                    if (scope is EditorContextScope scopeQuickFix)
                    {
                        scopeQuickFix.OnCommandExecuted(codeActionResult);
                    }

                    break;
                default:
                    _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest, $"{commandExecutedParams.Command} is not supported."));
                    break;
            }
        }

        private void HandleDidOpenNotification(string paramsJson)
        {
            Contracts.AssertValue(paramsJson);

            _logger?.Invoke($"[PFX] HandleDidOpenNotification: paramsJson={paramsJson ?? "<null>"}");

            if (!TryParseParams(paramsJson, out DidOpenTextDocumentParams didOpenParams))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(null, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = didOpenParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var expression = didOpenParams.TextDocument.Text;
            var result = scope.Check(expression);

            PublishDiagnosticsNotification(documentUri, expression, result.Errors.ToArray());

            PublishTokens(documentUri, result);

            PublishExpressionType(documentUri, result);
        }

        private void HandleDidChangeNotification(string paramsJson)
        {
            Contracts.AssertValue(paramsJson);

            _logger?.Invoke($"[PFX] HandleDidChangeNotification: paramsJson={paramsJson ?? "<null>"}");

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

            PublishDiagnosticsNotification(documentUri, expression, result.Errors.ToArray());

            PublishTokens(documentUri, result);

            PublishExpressionType(documentUri, result);
        }

        private void HandleCompletionRequest(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleCompletionRequest: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

            if (id == null)
            {
                _logger?.Invoke($"[PFX] HandleCompletionRequest: Invalid Request, id is null");
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return;
            }

            Contracts.AssertValue(id);
            Contracts.AssertValue(paramsJson);

            if (!TryParseParams(paramsJson, out CompletionParams completionParams))
            {
                _logger?.Invoke($"[PFX] HandleCompletionRequest: ParseError");
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return;
            }

            var documentUri = completionParams.TextDocument.Uri;
            var scope = _scopeFactory.GetOrCreateInstance(documentUri);

            var uri = new Uri(documentUri);
            var expression = GetExpression(completionParams, HttpUtility.ParseQueryString(uri.Query));
            if (expression == null)
            {
                _logger?.Invoke($"[PFX] HandleCompletionRequest: InvalidParams, expression is null");
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidParams));
                return;
            }

            var cursorPosition = GetPosition(expression, completionParams.Position.Line, completionParams.Position.Character);
            _logger?.Invoke($"[PFX] HandleCompletionRequest: calling Suggest...");
            var result = scope.Suggest(expression, cursorPosition);

            // Note: there are huge number of suggestions in initial requests
            // Including each of them in the log string is very expensive
            // Avoid this if possible
            _logger?.Invoke($"[PFX] HandleCompletionRequest: Suggest results: Count:{result.Suggestions.Count()}, Suggestions:{string.Join(", ", result.Suggestions.Select(s => $@"[{s.Kind}]: '{s.DisplayText.Text}'"))}");

            var precedingCharacter = cursorPosition != 0 ? expression[cursorPosition - 1] : '\0';
            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new
            {
                items = result.Suggestions.Select((item, index) => new CompletionItem()
                {
                    Label = item.DisplayText.Text,
                    Detail = item.FunctionParameterDescription,
                    Documentation = item.Definition,
                    Kind = GetCompletionItemKind(item.Kind),

                    // The order of the results should be preserved.  To do that, we embed the index
                    // into the sort text, which clients may sort lexicographically.
                    SortText = index.ToString("D3", CultureInfo.InvariantCulture),

                    // If the current position is in front of a single quote and the completion result starts with a single quote,
                    // we don't want to make it harder on the end user by inserting an extra single quote.
                    InsertText = item.DisplayText.Text is { } label && TexlLexer.IsIdentDelimiter(label[0]) && precedingCharacter == TexlLexer.IdentifierDelimiter ? label.Substring(1) : item.DisplayText.Text
                }),
                isIncomplete = false
            }));
        }

        private void HandleSignatureHelpRequest(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleSignatureHelpRequest: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

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
            var expression = GetExpression(signatureHelpParams, HttpUtility.ParseQueryString(uri.Query));
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
            _logger?.Invoke($"[PFX] HandleInitialFixupRequest: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

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
            expression = scope.ConvertToDisplay(expression);

            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new TextDocumentItem()
            {
                Uri = documentUri,
                Text = expression
            }));
        }

        // Base class handler for LSP commands. PArse message, dispatch, and handle error cases.
        private abstract class LSPHandler<TReq, TResp>
            where TReq : IHasTextDocument
        {
            protected readonly LanguageServer _parent;

            public LSPHandler(LanguageServer parent)
            {
                this._parent = parent;
            }

            public void Handle(string id, string paramsJson)
            {
                _parent._logger?.Invoke($"[PFX] HandleGetCapabilities: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

                if (id == null)
                {
                    _parent._sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                    return;
                }

                Contracts.AssertValue(id);
                Contracts.AssertValue(paramsJson);

                if (!_parent.TryParseParams(paramsJson, out TReq request))
                {
                    _parent._sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                    return;
                }

                var documentUri = request.TextDocument.Uri;
                var scope = _parent._scopeFactory.GetOrCreateInstance(documentUri);

                TResp result = this.Handle(scope, request);
                                
                _parent._sendToClient(JsonRpcHelper.CreateSuccessResult(id, result));
            }

            protected abstract TResp Handle(IPowerFxScope scope, TReq req);            
        }

        private class GetCapabilitiesHandler : LSPHandler<CustomGetCapabilitiesParams, CustomGetCapabilitiesResult>
        {
            public GetCapabilitiesHandler(LanguageServer parent)
                : base(parent)
            {
            }

            protected override CustomGetCapabilitiesResult Handle(IPowerFxScope scope, CustomGetCapabilitiesParams req)
            {
                var result = new CustomGetCapabilitiesResult();

                var nl = _parent.NL2FxImplementation;
                if (nl != null)
                {
                    result.SupportsNL2Fx = nl.SupportsNL2Fx;
                    result.SupportsFx2NL = nl.SupportsFx2NL;
                }

                return result;
            }
        }

        private class NL2FxHandler : LSPHandler<CustomNL2FxParams, CustomNL2FxResult>
        {
            public NL2FxHandler(LanguageServer parent)
                : base(parent)
            {
            }

            protected override CustomNL2FxResult Handle(IPowerFxScope scope, CustomNL2FxParams request)
            {
                var nl = _parent.NL2FxImplementation;
                if (nl == null || !nl.SupportsNL2Fx)
                {
                    throw new NotSupportedException($"NL2Fx not enabled");
                }

                var check = scope.Check("1"); // just need to get the symbols 
                var summary = check.ApplyGetContextSummary();

                var req = new NL2FxParameters
                {
                    Sentence = request.Sentence,
                    SymbolSummary = summary,
                    Engine = check.Engine
                };

                CancellationToken cancel = default;
                var result = _parent.NL2FxImplementation.NL2FxAsync(req, cancel)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                FinalCheck(scope, result);
                return result;
            }

            // This Engine / LSP context may have restrictions that aren't captured in
            // the NL2Fx payload, so the Model may have returned things that aren't valid here.
            // Do a final pass where we filter out any expressions that don't compile.
            public void FinalCheck(IPowerFxScope scope, CustomNL2FxResult result)
            {
                List<CustomNL2FxResultItem> items = new List<CustomNL2FxResultItem>();

                if (result.Expressions != null)
                {
                    foreach (var item in result.Expressions)
                    {
                        var check = scope.Check(item.Expression);
                        if (check.IsSuccess)
                        {
                            items.Add(item);
                        }
                    }
                }

                result.Expressions = items.ToArray();
            }
        }

        private class Fx2NLHandler : LSPHandler<CustomFx2NLParams, CustomFx2NLResult>
        {
            public Fx2NLHandler(LanguageServer parent)
            : base(parent)
            {
            }

            protected override CustomFx2NLResult Handle(IPowerFxScope scope, CustomFx2NLParams request)
            {
                var nl = _parent.NL2FxImplementation;
                if (nl == null || !nl.SupportsFx2NL)
                {
                    throw new NotSupportedException($"NL2Fx not enabled");
                }

                var check = scope.Check(request.Expression);

                CancellationToken cancel = default;
                var result = nl.Fx2NLAsync(check, cancel)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                return result;
            }
        }

        private void HandleCodeActionRequest(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleCodeActionRequest: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

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
            var expression = GetExpression(codeActionParams, HttpUtility.ParseQueryString(uri.Query));
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

                        if (scope is EditorContextScope scopeQuickFix)
                        {
                            var result = scopeQuickFix.SuggestFixes(expression, LogUnhandledExceptionHandler);

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
                                    },
                                    ActionResultContext = item.ActionResultContext
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

        /// <summary>
        /// Handles requests to compute semantic tokens for the full document or expression.
        /// </summary>
        /// <param name="id">Request Id.</param>
        /// <param name="paramsJson">Request Params Stringified Body.</param>
        private void HandleFullDocumentSemanticTokens(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleFullDocumentSemanticTokens: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

            if (!TryParseAndValidateSemanticTokenParams(id, paramsJson, out SemanticTokensParams semanticTokensParams))
            {
                return;
            }

            HandleSemanticTokens(id, semanticTokensParams, TextDocumentNames.FullDocumentSemanticTokens, true);
        }

        /// <summary>
        /// Handles requests to compute semantic tokens for the a specific part of document or expression.
        /// </summary>
        /// <param name="id">Request Id.</param>
        /// <param name="paramsJson">Request Params Stringified Body.</param>
        private void HandleRangeDocumentSemanticTokens(string id, string paramsJson)
        {
            _logger?.Invoke($"[PFX] HandleRangeDocumentSemanticTokens: id={id ?? "<null>"}, paramsJson={paramsJson ?? "<null>"}");

            if (!TryParseAndValidateSemanticTokenParams(id, paramsJson, out SemanticTokensRangeParams semanticTokensParams))
            {
                return;
            }

            if (semanticTokensParams.Range == null)
            {
                // No tokens for invalid range
                SendEmptySemanticTokensResponse(id);
                return;
            }

            HandleSemanticTokens(id, semanticTokensParams, TextDocumentNames.RangeDocumentSemanticTokens, false);
        }

        private void HandleSemanticTokens<T>(string id, T semanticTokensParams, string method, bool isFullDocument = false)
            where T : SemanticTokensParams
        {
            var uri = new Uri(semanticTokensParams.TextDocument.Uri);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var expression = GetExpression(semanticTokensParams, queryParams) ?? string.Empty;

            if (string.IsNullOrEmpty(expression))
            {
                // Empty tokens for the empty expression
                SendEmptySemanticTokensResponse(id);
                return;
            }

            // Monaco-Editor sometimes uses \r\n for the newline character. \n is not always the eol character so allowing clients to pass eol character
            var eol = queryParams?.Get("eol");
            eol = !string.IsNullOrEmpty(eol) ? eol : EOL.ToString();

            var startIndex = -1;
            var endIndex = -1;
            if (!isFullDocument)
            {
                (startIndex, endIndex) = (semanticTokensParams as SemanticTokensRangeParams).Range.ConvertRangeToPositions(expression, eol);
                if (startIndex < 0 || endIndex < 0)
                {
                    SendEmptySemanticTokensResponse(id);
                    return;
                }
            }

            var tokenTypesToSkip = ParseTokenTypesToSkipParam(queryParams?.Get("tokenTypesToSkip"));
            var scope = _scopeFactory.GetOrCreateInstance(semanticTokensParams.TextDocument.Uri);
            var result = scope?.Check(expression);
            if (result == null)
            {
                SendEmptySemanticTokensResponse(id);
                return;
            }

            var tokens = result.GetTokens(tokenTypesToSkip);

            if (!isFullDocument)
            {
                // Only consider overlapping tokens. end index is exlcusive
                tokens = tokens.Where(token => !(token.EndIndex <= startIndex || token.StartIndex >= endIndex));
            }

            var controlTokensObj = isFullDocument ? new ControlTokens() : null;

            var encodedTokens = SemanticTokensEncoder.EncodeTokens(tokens, expression, eol, controlTokensObj);
            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new SemanticTokensResponse() { Data = encodedTokens }));

            PublishControlTokenNotification(controlTokensObj, queryParams);
        }

        /// <summary>
        /// Handles publishing a control token notification if any control tokens found.
        /// </summary>
        /// <param name="controlTokensObj">Collection to add control tokens to.</param>
        /// <param name="queryParams">Collection of query params.</param>
        private void PublishControlTokenNotification(ControlTokens controlTokensObj, NameValueCollection queryParams)
        {
            if (controlTokensObj == null || queryParams == null)
            {
                return;
            }

            var version = queryParams.Get("version") ?? string.Empty;

            // Send PublishControlTokens notification
            _sendToClient(JsonRpcHelper.CreateNotification(
                CustomProtocolNames.PublishControlTokens,
                new PublishControlTokensParams()
                {
                    Version = version,
                    Controls = controlTokensObj.GetControlTokens()
                }));
        }

        private HashSet<TokenType> ParseTokenTypesToSkipParam(string rawTokenTypesToSkipParam)
        {
            var tokenTypesToSkip = new HashSet<TokenType>();
            if (string.IsNullOrWhiteSpace(rawTokenTypesToSkipParam))
            {
                return tokenTypesToSkip;
            }

            if (TryParseParams(rawTokenTypesToSkipParam, out List<int> tokenTypesToSkipParam))
            {
                foreach (var tokenTypeValue in tokenTypesToSkipParam)
                {
                    var tokenType = (TokenType)tokenTypeValue;
                    if (tokenType != TokenType.Lim)
                    {
                        tokenType = tokenType == TokenType.Min ? TokenType.Unknown : tokenType;
                        tokenTypesToSkip.Add(tokenType);
                    }
                }
            }

            return tokenTypesToSkip;
        }

        private bool TryParseAndValidateSemanticTokenParams<T>(string id, string paramsJson, out T semanticTokenParams)
            where T : SemanticTokensParams
        {
            semanticTokenParams = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest));
                return false;
            }

            if (!TryParseParams(paramsJson, out semanticTokenParams) || string.IsNullOrWhiteSpace(semanticTokenParams?.TextDocument.Uri))
            {
                _sendToClient(JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.ParseError));
                return false;
            }

            return true;
        }

        private void SendEmptySemanticTokensResponse(string id)
        {
            _sendToClient(JsonRpcHelper.CreateSuccessResult(id, new SemanticTokensResponse()));
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
        private DiagnosticSeverity DocumentSeverityToDiagnosticSeverityMap(ErrorSeverity severity) => severity switch
        {
            ErrorSeverity.Critical => DiagnosticSeverity.Error,
            ErrorSeverity.Severe => DiagnosticSeverity.Error,
            ErrorSeverity.Moderate => DiagnosticSeverity.Error,
            ErrorSeverity.Warning => DiagnosticSeverity.Warning,
            ErrorSeverity.Suggestion => DiagnosticSeverity.Hint,
            ErrorSeverity.Verbose => DiagnosticSeverity.Information,
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
                    diagnostics.Add(new Diagnostic()
                    {
                        Range = GetRange(expression, item.Span ?? new Span(0, 0)),
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

        /// <summary>
        /// Returns the expression from preferrably the request params or query params if not present in the request params.
        /// </summary>
        /// <param name="requestParams">Request params.</param>
        /// <param name="queryParams">Query Params.</param>
        /// <returns>Expression.</returns>
        private static string GetExpression(LanguageServerRequestBaseParams requestParams, NameValueCollection queryParams)
        {
            return requestParams?.Text ?? queryParams.Get("expression");
        }

        /// <summary>
        /// Construct a Range based on a Span for a given expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="span">The Span.</param>
        /// <returns>Generated Range.</returns>
        public static Range GetRange(string expression, Span span)
        {
            var startChar = GetCharPosition(expression, span.Min) - 1;
            var endChar = GetCharPosition(expression, span.Lim) - 1;

            var startCode = expression.Substring(0, span.Min);
            var code = expression.Substring(span.Min, span.Lim - span.Min);
            var startLine = startCode.Split(EOL).Length;
            var endLine = startLine + code.Split(EOL).Length - 1;

            var range = new Range()
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
            };

            Contracts.Assert(range.IsValid());

            return range;
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
        protected static int GetCharPosition(string expression, int position)
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
