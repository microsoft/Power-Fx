// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handles all kinds of semantic tokens operations.
    /// </summary>
    public class BaseSemanticTokensLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public virtual string LspMethod => TextDocumentNames.FullDocumentSemanticTokens;

        public bool IsRequest => true;

        private bool IsRangeSemanticTokens => LspMethod == TextDocumentNames.RangeDocumentSemanticTokens;

        public BaseSemanticTokensLanguageServerOperationHandler()
        {
        }

        /// <summary>
        /// Computes the semantic tokens for the given expression.
        /// Hook to allow consumers to override the token computation.
        /// </summary>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        /// <param name="getTokensContext"> Context that might be needed to compute tokens. </param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>A set of semantic tokens.</returns>
        // TODO: Need to revisit this at some point in future
        // TODO cont.: ITokenTextSpan is internal so this hook cannot be exposed
        // TODO cont.: Cannot make it public due to it being transport type
        private protected virtual async Task<IEnumerable<ITokenTextSpan>> GetTokensAsync(LanguageServerOperationContext operationContext, GetTokensContext getTokensContext, CancellationToken cancellationToken)
        {
            var result = await operationContext.CheckAsync(getTokensContext.documentUri, getTokensContext.expression, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            var tokens = result.GetTokens(getTokensContext.tokenTypesToSkip);
            return tokens;
        }

        /// <summary>
        /// Handles publishing a control token notification if any control tokens found.
        /// Overridable hook to allow consumers to avoid publishing control tokens if they do not support it.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="controlTokensObj">Collection to add control tokens to.</param>
        /// <param name="queryParams">Collection of query params.</param>
        protected virtual void PublishControlTokenNotification(LanguageServerOperationContext operationContext, ControlTokens controlTokensObj, NameValueCollection queryParams)
        {
            if (controlTokensObj == null || queryParams == null)
            {
                return;
            }

            var version = queryParams.Get("version") ?? string.Empty;

            operationContext.OutputBuilder.AddNotification(CustomProtocolNames.PublishControlTokens, new PublishControlTokensParams()
            {
                Version = version,
                Controls = controlTokensObj
            });
        }

        /// <summary>
        /// Handles the semantic tokens operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            var isRangeSemanticTokens = IsRangeSemanticTokens;
            operationContext.Logger?.LogInformation($"[PFX] {(isRangeSemanticTokens ? "HandleRangeSemanticTokens" : "HandleFullDocumentSemanticTokens")}: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!TryParseAndValidateSemanticTokenParams(operationContext, out var semanticTokensParams))
            {
                return;
            }

            var uri = new Uri(semanticTokensParams.TextDocument.Uri);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var expression = LanguageServerHelper.ChooseExpression(semanticTokensParams, queryParams) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(expression))
            {
                // Empty tokens for the empty expression
                WriteEmptySemanticTokensResponse(operationContext);
                return;
            }

            // Monaco-Editor sometimes uses \r\n for the newline character. \n is not always the eol character so allowing clients to pass eol character
            var eol = queryParams?.Get("eol");
            eol = !string.IsNullOrEmpty(eol) ? eol : PositionRangeHelper.EOL.ToString();

            var startIndex = -1;
            var endIndex = -1;
            if (isRangeSemanticTokens)
            {
                (startIndex, endIndex) = (semanticTokensParams as SemanticTokensRangeParams).Range.ConvertRangeToPositions(expression, eol);
                if (startIndex < 0 || endIndex < 0)
                {
                    WriteEmptySemanticTokensResponse(operationContext);
                    return;
                }
            }

            var tokenTypesToSkip = ParseTokenTypesToSkipParam(queryParams?.Get("tokenTypesToSkip"));
            var tokens = await GetTokensAsync(operationContext, new GetTokensContext(tokenTypesToSkip, semanticTokensParams.TextDocument.Uri, expression), cancellationToken).ConfigureAwait(false);

            if (tokens == null)
            {
                WriteEmptySemanticTokensResponse(operationContext);
                return;
            }

            if (isRangeSemanticTokens)
            {
                // Only consider overlapping tokens. end index is exlcusive
                tokens = tokens.Where(token => !(token.EndIndex <= startIndex || token.StartIndex >= endIndex));
            }

            var controlTokensObj = !isRangeSemanticTokens ? new ControlTokens() : null;

            var encodedTokens = SemanticTokensEncoder.EncodeTokens(tokens, expression, eol, controlTokensObj);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new SemanticTokensResponse() { Data = encodedTokens });
            PublishControlTokenNotification(operationContext, controlTokensObj, queryParams);
        }

        /// <summary>
        /// Attempts to parse and validate the semantic token parameters.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="semanticTokenParams">Reference to hold the parsed semantic token params.</param>
        /// <returns>True if parsing and validation was successful or false otherwise.</returns>
        private bool TryParseAndValidateSemanticTokenParams(LanguageServerOperationContext operationContext, out SemanticTokensParams semanticTokenParams)
        {
            semanticTokenParams = null;
            SemanticTokensRangeParams semanticTokensRangeParams = null;

            var parseResult = false;
            if (IsRangeSemanticTokens)
            {
                parseResult = operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out semanticTokensRangeParams);
                semanticTokenParams = semanticTokensRangeParams;
            }
            else
            {
                parseResult = operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out semanticTokenParams);
            }

            if (!parseResult)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(semanticTokenParams?.TextDocument.Uri))
            {
                operationContext.OutputBuilder.AddParseError(operationContext.RequestId, "Invalid document uri for semantic tokens operation");
                return false;
            }

            if (IsRangeSemanticTokens && semanticTokensRangeParams?.Range == null)
            {
                WriteEmptySemanticTokensResponse(operationContext);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the token types to skip parameter.
        /// These are the token types that should be skipped from the semantic tokens response.
        /// </summary>
        /// <param name="rawTokenTypesToSkipParam">Raw token types to skip parameter.</param>
        /// <returns>Parsed token types to skip parameter.</returns>
        private static HashSet<TokenType> ParseTokenTypesToSkipParam(string rawTokenTypesToSkipParam)
        {
            var tokenTypesToSkip = new HashSet<TokenType>();
            if (string.IsNullOrWhiteSpace(rawTokenTypesToSkipParam))
            {
                return tokenTypesToSkip;
            }

            if (LanguageServerHelper.TryParseParams(rawTokenTypesToSkipParam, out List<int> tokenTypesToSkipParam))
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

        /// <summary>
        /// Adds the empty semantic tokens response to the output builder.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        private static void WriteEmptySemanticTokensResponse(LanguageServerOperationContext operationContext)
        {
           operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new SemanticTokensResponse());
        }
    }
}
