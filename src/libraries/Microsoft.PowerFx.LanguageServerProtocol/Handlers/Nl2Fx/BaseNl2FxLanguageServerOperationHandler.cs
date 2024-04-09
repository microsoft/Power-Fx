// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// An abstract handler for NL2FX operations.
    /// This is a class and not interface to allow us to add new methods in future without breaking existing implementations.
    /// </summary>
    public class BaseNl2FxLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.NL2FX;

        /// <summary>
        /// Performs pre-handle operations for NL2FX.
        /// </summary>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        /// <param name="handleContext">  Context for handling NL2Fx operation.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> Nl2Fx Handle Context extended with pre-handle result. </returns>
        protected virtual async Task<Nl2FxHandleContext> PreHandleNl2FxAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext,  CancellationToken cancellationToken)
        {
            var check = await operationContext.CheckAsync(handleContext.nl2FxRequestParams.TextDocument.Uri, LanguageServer.Nl2FxDummyFormula, cancellationToken).ConfigureAwait(false) ?? throw new NullReferenceException("Check result was not found for NL2Fx operation");
            var summary = check.ApplyGetContextSummary();
            var nl2FxParameters = new NL2FxParameters
            {
                Sentence = handleContext.nl2FxRequestParams.Sentence,
                SymbolSummary = summary,
                Engine = check.Engine
            };

            return handleContext with { preHandleResult = new Nl2FxPreHandleResult(nl2FxParameters) };
         }

        /// <summary>
        /// Performs Core NL2FX operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="handleContext">  Context for handling NL2Fx operation.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> Nl2Fx Handle Context extended with NL2Fx result. </returns>
        protected virtual async Task<Nl2FxHandleContext> Nl2FxAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext, CancellationToken cancellationToken)
        {
            return handleContext with { nl2FxResult = new Nl2FxResult(new CustomNL2FxResult()) };
        }

        /// <summary>
        /// Performs post-handle operations for NL2FX.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="handleContext">  Context for handling NL2Fx operation.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Trye if operation was successful or false.</returns>
        protected virtual async Task PostHandleNl2FxResultsAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext, CancellationToken cancellationToken)
        {
            var nl2FxResult = handleContext?.nl2FxResult?.actualResult;
            if (nl2FxResult?.Expressions != null)
            {
                foreach (var item in nl2FxResult.Expressions)
                {
                    if (item?.Expression == null)
                    {
                        continue;
                    }

                    var check = await operationContext.CheckAsync(handleContext.nl2FxRequestParams.TextDocument.Uri, item.Expression, cancellationToken).ConfigureAwait(false);
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

            var handleContext = new Nl2FxHandleContext(nl2FxRequestParams, null, null);

            // Each stage incremntally adds more information to the handleContext.
            // HandleContext is immutable and each stage creates a new instance of handleContext with more information.
            handleContext = await PreHandleNl2FxAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
            if (handleContext.preHandleResult == null)
            {
                return;
            }

            handleContext = await Nl2FxAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
            if (handleContext.nl2FxResult == null)
            {
                return;
            }

            await PostHandleNl2FxResultsAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
