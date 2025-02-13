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

        private readonly CustomNL2FxParams _nl2FxRequestParams;

        public Nl2FxLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        /// <summary>
        /// Performs pre-handle operations for NL2FX.
        /// </summary>
        /// <param name="handler"> Custom Nl Handler provided by the consumer of the Language Server. </param>
        /// <param name="requestNl2FxParams"> Nl2Fx params sent by client. </param>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Nl2Fx Parameters computed from the client request.</returns>
        private static Task<NL2FxParameters> PreHandleNl2Fx(NLHandler handler, CustomNL2FxParams requestNl2FxParams, LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            return operationContext.ExecuteHostTaskAsync(
            requestNl2FxParams.TextDocument.Uri,
            (scope) =>
            {
                var nl2FxParameters = scope is IPowerFxScopeNL2Fx nL2FxScope ? nL2FxScope.GetNL2FxParameters() : new NL2FxParameters();
                nl2FxParameters.FeatureName ??= requestNl2FxParams.FeatureName;
                nl2FxParameters.Sentence = requestNl2FxParams.Sentence;

                if (!handler.SkipDefaultPreHandleForNl2Fx)
                {
                    var check = scope?.Check(LanguageServer.Nl2FxDummyFormula) ?? throw new NullReferenceException("Check result was not found for NL2Fx operation");
                    var summary = check.ApplyGetContextSummary();
                    nl2FxParameters.SymbolSummary = summary;
                    nl2FxParameters.ExpressionLocale = check.ParserCultureInfo;
                    nl2FxParameters.Engine = check.Engine;
                }

                handler.PreHandleNl2Fx(requestNl2FxParams, nl2FxParameters, operationContext);
                return Task.FromResult(nl2FxParameters);
            },
            cancellationToken);
        }

        /// <summary>
        /// Performs Core NL2FX operation.
        /// </summary>
        /// <param name="nL2FxParameters">Nl2Fx Parameters.</param>
        /// <param name="nlHandler">Custom Nl Handler.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> Nl2Fx Model Call Result. </returns>
        private static Task<CustomNL2FxResult> Nl2FxAsync(NL2FxParameters nL2FxParameters, NLHandler nlHandler, LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            return nlHandler.NL2FxAsync(nL2FxParameters, cancellationToken);
        }

        /// <summary>
        /// Performs post-handle operations for NL2FX.
        /// </summary>
        /// <param name="nl2FxResult">Nl2Fx model call result.</param>
        /// <param name="scope">PowerFx Scope.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        private static void PostHandleNl2FxResults(CustomNL2FxResult nl2FxResult, IPowerFxScope scope, LanguageServerOperationContext operationContext)
        {
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
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out CustomNL2FxParams nl2FxRequestParams))
            {
                return;
            }

            var nlHandler = await operationContext.GetNLHandlerAsync(nl2FxRequestParams.TextDocument.Uri, _nLHandlerFactory, nl2FxRequestParams, cancellationToken).ConfigureAwait(false);
            if (!nlHandler.SupportsNL2Fx)
            {
                throw new NotSupportedException("Nl2fx is not supported");
            }

            var nl2FxParamters = await PreHandleNl2Fx(nlHandler, nl2FxRequestParams, operationContext, cancellationToken).ConfigureAwait(false);
            if (nl2FxParamters == null)
            {
                return;
            }

            var nl2FxResult = await Nl2FxAsync(nl2FxParamters, nlHandler, operationContext, cancellationToken).ConfigureAwait(false);
            if (nl2FxResult == null)
            {
                return;
            }

            await operationContext.ExecuteHostTaskAsync(nl2FxRequestParams.TextDocument.Uri, (scope) => PostHandleNl2FxResults(nl2FxResult, scope, operationContext), cancellationToken).ConfigureAwait(false);
        }
    }
}
