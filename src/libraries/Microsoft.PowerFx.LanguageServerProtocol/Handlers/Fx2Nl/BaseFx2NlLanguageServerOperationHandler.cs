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
    public class BaseFx2NlLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.FX2NL;

        protected CheckResult _checkResultFromInputExpresion;
        protected Fx2NLParameters _fx2NlParameters;
        protected CustomFx2NLParams _fx2NlRequestParams;

        /// <summary>
        /// Performs pre-handle operations for Fx2Nl.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Result of pre handle operations for Fx2Nl.</returns>
        protected virtual async Task<bool> PreHandleFx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            _checkResultFromInputExpresion = await operationContext.CheckAsync(_fx2NlRequestParams.TextDocument.Uri, _fx2NlRequestParams.Expression, cancellationToken).ConfigureAwait(false) ?? throw new NullReferenceException("Check result was not found for Fx2NL operation");
            _fx2NlParameters = GetFx2NlHints(operationContext);
            return true;
        }

        /// <summary>
        /// Gets the Fx2Nl hints.
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// </summary>
        /// <returns>Fx2Nl hints.</returns>
        protected virtual Fx2NLParameters GetFx2NlHints(LanguageServerOperationContext operationContext)
        {
            var scope = operationContext.GetScope(_fx2NlRequestParams.TextDocument.Uri);
            return scope is IPowerFxScopeFx2NL fx2NLScope ? fx2NLScope.GetFx2NLParameters() : new Fx2NLParameters();
        }

        /// <summary>
        /// Performs core Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns></returns>
        protected virtual async Task<bool> Fx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {   
            return true;
        }

        /// <summary>
        /// Orchestrates and handles the Fx2Nl operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!ParseAndValidateParams(operationContext))
            {
                return;
            }

            if (!await PreHandleFx2NlAsync(operationContext, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await Fx2NlAsync(operationContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and validates the Fx2Nl Request parameters.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <returns>True if parsing and validation succeeds or false otherwise.</returns>
        private bool ParseAndValidateParams(LanguageServerOperationContext operationContext)
        {
            return operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out _fx2NlRequestParams);
        }
    }
}
