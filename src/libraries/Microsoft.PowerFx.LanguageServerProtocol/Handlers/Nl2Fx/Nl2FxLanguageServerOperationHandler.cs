// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// An abstract handler for NL2FX operations.
    /// This is a class and not interface to allow us to add new methods in future without breaking existing implementations.
    /// </summary>
    internal sealed class Nl2FxLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.NL2FX;

        private readonly INLHandlerFactory _nLHandlerFactory;

        private CustomNL2FxParams _nl2FxRequestParams;

        private NLHandler _nlHandler;

        private CustomNL2FxResult _nl2FxResult;

        private NL2FxParameters _nl2FxParameters;

        public Nl2FxLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        /// <summary>
        /// Performs pre-handle operations for NL2FX.
        /// </summary>
        /// <param name="scope"> PowerFx Scope. </param>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        private void PreHandleNl2Fx(IPowerFxScope scope, LanguageServerOperationContext operationContext)
        {
            _nlHandler = operationContext.GetNLHandler(_nl2FxRequestParams.TextDocument.Uri, _nLHandlerFactory, _nl2FxRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Nl2Fx");
            if (!_nlHandler.SupportsNL2Fx)
            {
                throw new NotSupportedException("Nl2fx is not supported");
            }

            var check = scope?.Check(LanguageServer.Nl2FxDummyFormula) ?? throw new NullReferenceException("Check result was not found for NL2Fx operation");
            var summary = check.ApplyGetContextSummary();
            _nl2FxParameters = new NL2FxParameters
            {
                Sentence = _nl2FxRequestParams.Sentence,
                SymbolSummary = summary,
                Engine = check.Engine,
                ExpressionLocale = check.ParserCultureInfo
            };
         }

        /// <summary>
        /// Performs Core NL2FX operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> Nl2Fx Handle Context extended with NL2Fx result. </returns>
        private async Task Nl2FxAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            _nl2FxResult = await _nlHandler.NL2FxAsync(_nl2FxParameters, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs post-handle operations for NL2FX.
        /// </summary>
        /// <param name="scope">PowerFx Scope.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        private void PostHandleNl2FxResults(IPowerFxScope scope, LanguageServerOperationContext operationContext)
        {
            var nl2FxResult = _nl2FxResult;
            if (nl2FxResult?.Expressions != null)
            {
                foreach (var item in nl2FxResult.Expressions)
                {
                    if (item?.Expression == null)
                    {
                        continue;
                    }

                    var check = scope?.Check(item.Expression);
                    if (check != null && !check.IsSuccess)
                    {
                        item.RawExpression = item.Expression;
                        item.Expression = null;
                    }

                    item.AnonymizedExpression = check?.ApplyGetLogging();
                }
            }

            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, nl2FxResult);
        }

        /// <summary>
        /// Orchestrates and handles the NL2Fx operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out _nl2FxRequestParams))
            {
                return;
            }

            await operationContext.ExecuteHostTaskAsync(
            _nl2FxRequestParams.TextDocument.Uri,   
            (scope) => 
            {
                PreHandleNl2Fx(scope, operationContext);
                _nlHandler.PreHandleNl2Fx(_nl2FxRequestParams, _nl2FxParameters, operationContext);
            }, 
            cancellationToken).ConfigureAwait(false);
            if (_nl2FxParameters == null)
            {
                return;
            }

            await Nl2FxAsync(operationContext, cancellationToken).ConfigureAwait(false);
            if (_nl2FxResult == null)
            {
                return;
            }

            await operationContext.ExecuteHostTaskAsync(_nl2FxRequestParams.TextDocument.Uri, (scope) => PostHandleNl2FxResults(scope, operationContext), cancellationToken).ConfigureAwait(false);
        }
    }
}
