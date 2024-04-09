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
    public abstract class BaseFx2NlLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.FX2NL;

        /// <summary>
        /// Performs pre-handle operations for Fx2Nl.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="handleContext">Fx2Nl Handle Context.</param>
        /// <returns>Fx2Nl Handle Context extended with pre-handle result.</returns>
        protected virtual async Task<Fx2NlHandleContext> PreHandleFx2NlAsync(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext, CancellationToken cancellationToken)
        {
            var checkResult = await operationContext.CheckAsync(handleContext.fx2NlRequestParams.TextDocument.Uri, handleContext.fx2NlRequestParams.Expression, cancellationToken).ConfigureAwait(false) ?? throw new NullReferenceException("Check result was not found for Fx2NL operation");
            var fx2NlParameters = GetFx2NlHints(operationContext, handleContext);
            return handleContext with { preHandleResult = new Fx2NlPreHandleResult(checkResult, fx2NlParameters) };
        }

        /// <summary>
        /// Gets the Fx2Nl hints.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="handleContext">Fx2Nl Handle Context.</param>
        /// <returns>Fx2Nl hints.</returns>
        protected virtual Fx2NLParameters GetFx2NlHints(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext)
        {
            var scope = operationContext.GetScope(handleContext.fx2NlRequestParams.TextDocument.Uri);
            return scope is IPowerFxScopeFx2NL fx2NLScope ? fx2NLScope.GetFx2NLParameters() : new Fx2NLParameters();
        }

        /// <summary>
        /// Performs core Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="handleContext">Fx2Nl Handle Context.</param>
        /// <returns> Extended Fx2Nl Handle Contex.</returns>
        protected abstract Task<Fx2NlHandleContext> Fx2NlAsync(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext, CancellationToken cancellationToken);

        /// <summary>
        /// Orchestrates and handles the Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out CustomFx2NLParams fx2NlRequestParams))
            {
                return;
            }

            var handleContext = new Fx2NlHandleContext(fx2NlRequestParams, null);

            handleContext = await PreHandleFx2NlAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
            if (handleContext.preHandleResult == null)
            {
                return;
            }

            await Fx2NlAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
