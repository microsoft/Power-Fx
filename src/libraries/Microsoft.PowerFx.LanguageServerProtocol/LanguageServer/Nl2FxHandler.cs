// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public abstract class Fx2NLHandler : BaseLanguageServerOperationHandler<CustomFx2NLParams>
    {
        protected CheckResult _preHandleCheckResult;

        protected CustomFx2NLResult _fx2NLResult;

        protected Fx2NLParameters _fx2NLParameters;

        protected override async Task<bool> PreHandleAsync()
        {
            this._preHandleCheckResult = await CheckAsync(_operationContext.operationInput.Expression).ConfigureAwait(false);
            this._fx2NLParameters = ProvideFx2NLParameters();
            return true;
        }

        protected override async Task<bool> HandleOperationAsync()
        {
            this._fx2NLResult = await Fx2NlAsync().ConfigureAwait(false);
            _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, _fx2NLResult);
            return true;
        }

        protected abstract Task<CustomFx2NLResult> Fx2NlAsync();

        protected virtual Fx2NLParameters ProvideFx2NLParameters()
        {
            return null;
        }
    }

    public abstract class BaseNl2FxHandler : BaseLanguageServerOperationHandler<CustomNL2FxParams>
    {
        /// <summary>
        /// This const represents the dummy formula that is used to create an infrastructure needed to get the symbols for Nl2Fx operation.
        /// </summary>
        public static readonly string Nl2FxDummyFormula = "\"f7979178-07f0-424d-8f8b-00fee6fd19b8\"";

        protected NL2FxParameters _nl2FxParameters;

        protected CustomNL2FxResult _nl2FxResult;

        public BaseNl2FxHandler() 
        {
        }

        protected override async Task<bool> PreHandleAsync()
        {
            var checkResult = await CheckAsync(Nl2FxDummyFormula).ConfigureAwait(false);
            var summary = checkResult.ApplyGetContextSummary();

            this._nl2FxParameters = new NL2FxParameters
            {
                Sentence = _operationContext.operationInput.Sentence,
                SymbolSummary = summary,
                Engine = checkResult.Engine
            };

            return true;
        }

        protected override async Task<bool> PostHandleAsync()
        {
            var result = _nl2FxResult;
            if (result?.Expressions != null)
            {
                foreach (var item in result.Expressions)
                {
                    var check = await CheckAsync(item.Expression).ConfigureAwait(false);
                    if (!check.IsSuccess)
                    {
                        item.RawExpression = item.Expression;
                        item.Expression = null;
                    }
                }
            }

            _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, _nl2FxResult);
            return true;
        }

        protected override async Task<bool> HandleOperationAsync()
        {
            this._nl2FxResult = await Nl2FxAsync().ConfigureAwait(false);
            return true;
        }

        protected abstract Task<CustomNL2FxResult> Nl2FxAsync();
    }

    public abstract class SemanticTokensHandler<T> : BaseLanguageServerOperationHandler<T>
        where T : SemanticTokensParams
    {
        protected string _expression;

        private protected IEnumerable<ITokenTextSpan> _tokens;

        protected string _eol;

        protected NameValueCollection _uriParts;

        protected override async Task<bool> PreHandleAsync()
        {
            _uriParts = HttpUtility.ParseQueryString(new Uri(_operationContext.operationInput.TextDocument.Uri).Query);
            _expression = GetExpression(_operationContext.operationInput, _uriParts);
            if (string.IsNullOrWhiteSpace(_expression))
            {
                _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, new SemanticTokensResponse());
                return false;
            }

            var eol = _uriParts?.Get("eol");
            _eol = !string.IsNullOrEmpty(eol) ? eol : '\n'.ToString();
            return true;
        }

        protected override async Task<bool> HandleOperationAsync()
        {
            var tokenTypesToSkip = ParseTokenTypesToSkipParam(_uriParts?.Get("tokenTypesToSkip"));
            var checkResult = await CheckAsync(_expression).ConfigureAwait(false);
            if (checkResult == null)
            {
                _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, new SemanticTokensResponse());
                return false;
            }

            this._tokens = checkResult.GetTokens(tokenTypesToSkip);
            return true;
        }

        protected override async Task<bool> PostHandleAsync()
        {
            var controlTokens = GetControlTokensBag();
            var semanticTokens = SemanticTokensEncoder.EncodeTokens(_tokens, _expression, _eol, controlTokens);
            _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, new SemanticTokensResponse() { Data = semanticTokens });

            if (controlTokens != null && _uriParts != null)
            {
                var version = _uriParts.Get("version") ?? string.Empty;
                _operationContext.languageServerResponseBuilder.AddNotification(
                CustomProtocolNames.PublishControlTokens,
                new PublishControlTokensParams()
                {
                    Version = version,
                    Controls = controlTokens.GetControlTokens()
                });
            }

            return true;
        }

        private protected virtual ControlTokens GetControlTokensBag()
        {
            return new ControlTokens();
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
    }

    public abstract class RangeSemanticTokensHandler : SemanticTokensHandler<SemanticTokensRangeParams>
    {
        private int _startIndex = -1;

        private int _endIndex = -1;

        protected override async Task<bool> PreHandleAsync()
        {
            if (!(await base.PreHandleAsync().ConfigureAwait(false)))
            {
                return false;
            }

            (_startIndex, _endIndex) = _operationContext.operationInput.Range.ConvertRangeToPositions(_expression, _eol);
            if (_startIndex < 0 || _endIndex < 0)
            {
                _operationContext.languageServerResponseBuilder.AddSuccessResponse(_operationContext.id, new SemanticTokensResponse());
                return false;
            }

            return true;
        }

        protected override Task<bool> PostHandleAsync()
        {
            this._tokens = this._tokens.Where(token => !(token.EndIndex <= _startIndex || token.StartIndex >= _endIndex));
            return base.PostHandleAsync();
        }

        private protected override ControlTokens GetControlTokensBag()
        {
            return null;
        }
    }

    public abstract class FullSemanticTokensHandler : SemanticTokensHandler<SemanticTokensParams>
    {
    }
}
