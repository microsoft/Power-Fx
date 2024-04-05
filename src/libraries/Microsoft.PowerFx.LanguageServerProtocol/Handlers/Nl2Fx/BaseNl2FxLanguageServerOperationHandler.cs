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
    public class BaseNl2FxLanguageServerOperationHandler : BaseLanguageServerOperationHandler
    {
        protected NL2FxParameters _nl2FxParameters;
        protected CustomNL2FxResult _nl2FxResult;
        protected CustomNL2FxParams _nl2FxRequestParams;

        public override string LspMethod => CustomProtocolNames.NL2FX;

        /// <summary>
        /// Performs pre-handle operations for NL2FX.
        /// </summary>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>True if operation was successful or false.</returns>
        protected virtual async Task<bool> PreHandleNl2FxAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            var check = await operationContext.CheckAsync(_nl2FxRequestParams.TextDocument.Uri, LanguageServer.Nl2FxDummyFormula, cancellationToken).ConfigureAwait(false) ?? throw new NullReferenceException("Check result was not found for NL2Fx operation");
            var summary = check.ApplyGetContextSummary();
            _nl2FxParameters = new NL2FxParameters
            {
                Sentence = _nl2FxRequestParams.Sentence,
                SymbolSummary = summary,
                Engine = check.Engine
            };

            return true;  
         }

        /// <summary>
        /// Performs Core NL2FX operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> True if operation was successful or false.</returns>
        protected virtual async Task<bool> Nl2FxAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            _nl2FxResult = new CustomNL2FxResult();
            return true;
        }

        /// <summary>
        /// Performs post-handle operations for NL2FX.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Trye if operation was successful or false.</returns>
        protected virtual async Task<bool> PostHandleNl2FxResultsAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (_nl2FxResult?.Expressions != null)
            {
                foreach (var item in _nl2FxResult.Expressions)
                {
                    if (item?.Expression == null)
                    {
                        continue;
                    }

                    var check = await operationContext.CheckAsync(_nl2FxRequestParams.TextDocument.Uri, item.Expression, cancellationToken).ConfigureAwait(false);
                    if (check != null && !check.IsSuccess)
                    {
                        item.RawExpression = item.Expression;
                        item.Expression = null;
                    }
                }
            }

            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, _nl2FxResult);
            return true;
        }

        /// <summary>
        /// Parses and validates the parameters for NL2Fx operation.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <returns>True if parsing and validation was success or false otherwise.</returns>
        protected bool ParseAndValidateParams(LanguageServerOperationContext operationContext)
        {
            return operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out _nl2FxRequestParams);
        }

        /// <summary>
        /// Orchestrates and handles the NL2Fx operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public sealed override async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!ParseAndValidateParams(operationContext))
            {
                return;
            }

            if (!await PreHandleNl2FxAsync(operationContext, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            if (!await Nl2FxAsync(operationContext, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await PostHandleNl2FxResultsAsync(operationContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
