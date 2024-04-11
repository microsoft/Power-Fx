// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{ 
    /// <summary>
    /// An abstract handler for Fx2Nl operations.
    /// This is a class and not interface to allow us to add new methods in future without breaking existing implementations.
    /// </summary>
    public class Fx2NlLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.FX2NL;

        private readonly INLHandlerFactory _nLHandlerFactory;

        private CheckResult _preHandleCheckResult;

        private Fx2NLParameters _fx2NlParameters;

        private CustomFx2NLParams _fx2NlRequestParams;

        private NLHandler _nLHandler;

        public Fx2NlLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        /// <summary>
        /// Performs pre-handle operations for Fx2Nl.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        private Task PreHandleFx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            _nLHandler = _nLHandlerFactory?.GetNLHandler(operationContext.GetScope(_fx2NlRequestParams.TextDocument.Uri), _fx2NlRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Fx2Nl");
            if (!_nLHandler.SupportsFx2NL)
            {
                throw new NotSupportedException("Fx2Nl is not supported");
            }

            return operationContext.ExecuteHostTaskAsync(
            () =>
            {
                _preHandleCheckResult = operationContext.Check(_fx2NlRequestParams.TextDocument.Uri, _fx2NlRequestParams.Expression) ?? throw new NullReferenceException("Check result was not found for Fx2NL operation");
                _fx2NlParameters = GetFx2NlHints(operationContext);
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the Fx2Nl hints.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <returns>Fx2Nl hints.</returns>
        private Fx2NLParameters GetFx2NlHints(LanguageServerOperationContext operationContext)
        {
            var scope = operationContext.GetScope(_fx2NlRequestParams.TextDocument.Uri);
            return scope is IPowerFxScopeFx2NL fx2NLScope ? fx2NLScope.GetFx2NLParameters() : new Fx2NLParameters();
        }

        /// <summary>
        /// Performs core Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        private async Task Fx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            var result = await _nLHandler.Fx2NLAsync(_preHandleCheckResult, _fx2NlParameters, cancellationToken).ConfigureAwait(false);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, result);
        }

        /// <summary>
        /// Orchestrates and handles the Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out _fx2NlRequestParams))
            {
                return;
            }

            await PreHandleFx2NlAsync(operationContext, cancellationToken).ConfigureAwait(false);
            if (_preHandleCheckResult == null)
            {
                return;
            }

            await Fx2NlAsync(operationContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
