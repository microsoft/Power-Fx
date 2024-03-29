// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public interface ILanguageServerOperationHandlerFactory
    {
        ILanguageServerOperationHandler GetHandler(string method);
    }

    public class LanguageServerV2
    { 
        private readonly ILanguageServerOperationHandlerFactory _factory;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        public LanguageServerV2(ILanguageServerOperationHandlerFactory factory)
        {
            _factory = factory;
        }

        public async Task<string> HandleOperationAsync(string jsonRpcPayload)
        {
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
                        return JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest);
                    }

                    if (!element.TryGetProperty("params", out var paramsElement))
                    {
                        return JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InvalidRequest);
                    }

                    var method = methodElement.GetString();
                    var paramsJson = paramsElement.GetRawText();
                    var handler = _factory.GetHandler(method);
                    if (handler == null)
                    {
                        return JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.MethodNotFound);
                    }

                    var languageServerResponseBuilder = new LanguageServerResponseBuilder();
                    switch (method)
                    {
                        case TextDocumentNames.FullDocumentSemanticTokens:
                            await HandleFullSemanticTokens(id, paramsJson, handler, languageServerResponseBuilder).ConfigureAwait(false);
                            break;
                        case TextDocumentNames.RangeDocumentSemanticTokens:
                            await HandleRangeSemanticTokens(id, paramsJson, handler, languageServerResponseBuilder).ConfigureAwait(false);
                            break;
                        case CustomProtocolNames.NL2FX:
                            await HandleNl2Fx(id, paramsJson, handler, languageServerResponseBuilder).ConfigureAwait(false);
                            break;
                        case CustomProtocolNames.FX2NL:
                            await HandleFx2Nl(id, paramsJson, handler, languageServerResponseBuilder).ConfigureAwait(false);
                            break;
                        default:
                            return JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.MethodNotFound);
                    }

                    return languageServerResponseBuilder.Response;
                }
            }
            catch (System.Exception e)
            {
                return JsonRpcHelper.CreateErrorResult(id, JsonRpcHelper.ErrorCode.InternalError, e.GetDetailedExceptionMessage());
            }
        }

        private async Task HandleFullSemanticTokens(string id, string paramsJson, ILanguageServerOperationHandler handler, LanguageServerResponseBuilder builder)
        {
            if (!TryParseAndValidateSemanticTokenParams(id, paramsJson, builder, out SemanticTokensParams semanticTokensParams))
            {
                return;
            }

            await handler.HandleAsync(new LanguageServerOperationContext<SemanticTokensParams>(id, semanticTokensParams, builder)).ConfigureAwait(false);
        }

        private async Task HandleRangeSemanticTokens(string id, string paramsJson, ILanguageServerOperationHandler handler, LanguageServerResponseBuilder builder)
        {
            if (!TryParseAndValidateSemanticTokenParams(id, paramsJson, builder, out SemanticTokensRangeParams semanticTokensParams))
            {
                return;
            }

            await handler.HandleAsync(new LanguageServerOperationContext<SemanticTokensRangeParams>(id, semanticTokensParams, builder)).ConfigureAwait(false);
        }

        private async Task HandleNl2Fx(string id, string paramsJson, ILanguageServerOperationHandler handler, LanguageServerResponseBuilder builder)
        {
            if (!TryParseParams(paramsJson, out CustomNL2FxParams nl2FxParams))
            {
                return;
            }

            await handler.HandleAsync(new LanguageServerOperationContext<CustomNL2FxParams>(id, nl2FxParams, builder)).ConfigureAwait(false);
        }

        private async Task HandleFx2Nl(string id, string paramsJson, ILanguageServerOperationHandler handler, LanguageServerResponseBuilder builder)
        {
            if (!TryParseParams(paramsJson, out CustomFx2NLParams fx2NLParams))
            {
                return;
            }

            await handler.HandleAsync(new LanguageServerOperationContext<CustomFx2NLParams>(id, fx2NLParams, builder)).ConfigureAwait(false);
        }

        private bool TryParseAndValidateSemanticTokenParams<T>(string id, string paramsJson, LanguageServerResponseBuilder builder, out T semanticTokenParams)
            where T : SemanticTokensParams
        {
            semanticTokenParams = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                builder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.InvalidRequest);
                return false;
            }

            if (!TryParseParams(paramsJson, out semanticTokenParams) || string.IsNullOrWhiteSpace(semanticTokenParams?.TextDocument.Uri))
            {
                builder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.ParseError);
                return false;
            }

            return true;
        }

        private static bool TryParseParams<T>(string json, out T result)
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
    }
}
