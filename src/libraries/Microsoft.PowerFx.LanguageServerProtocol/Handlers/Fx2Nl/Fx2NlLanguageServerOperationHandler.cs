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
    /// An abstract handler for Fx2Nl operations.
    /// This is a class and not interface to allow us to add new methods in future without breaking existing implementations.
    /// </summary>
    internal sealed class Fx2NlLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.FX2NL;

        private readonly INLHandlerFactory _nLHandlerFactory;

        public Fx2NlLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        /// <summary>
        /// Performs pre-handle operations for Fx2Nl.
        /// </summary>
        /// <param name="fx2NlRequestParams">Fx2nl Parameters sent by client.</param>
        /// <param name="nlHandler">Custom Nl Handler.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Fx2nl parameters and check result derived from host and client provided parameters.</returns>
        private static Task<(Fx2NLParameters fx2NLParameters, CheckResult preHandleCheckResult)> PreHandleFx2NlAsync(CustomFx2NLParams fx2NlRequestParams, NLHandler nlHandler, LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            return operationContext.ExecuteHostTaskAsync(
            fx2NlRequestParams.TextDocument.Uri,
            (scope) =>
            {
                var preHandleCheckResult = scope?.Check(fx2NlRequestParams.Expression) ?? throw new NullReferenceException("Check result was not found for Fx2NL operation");
                var fx2NlParameters = GetFx2NlHints(fx2NlRequestParams, scope, operationContext);

                return Task.FromResult((fx2NlParameters, preHandleCheckResult));    
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the Fx2Nl hints including Usage hints and optional range.
        /// </summary>
        /// <param name="customFx2NLParams">Fx2nl params sent by client.</param>
        /// <param name="scope"> PowerFx Scope.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <returns>Fx2Nl hints.</returns>
        private static Fx2NLParameters GetFx2NlHints(CustomFx2NLParams customFx2NLParams, IPowerFxScope scope,  LanguageServerOperationContext operationContext)
        {
            var parameters = scope is IPowerFxScopeFx2NL fx2NLScope ? fx2NLScope.GetFx2NLParameters() : new Fx2NLParameters();
            parameters.Range ??= customFx2NLParams.Range;

            return parameters;
        }

        /// <summary>
        /// Performs core Fx2Nl operation.
        /// </summary>
        /// <param name="fx2NLParameters">Fx2nl parameters derived from host and client provided parameters.</param>
        /// <param name="nlHandler">Custom Nl Handler.</param>
        /// <param name="preHandleCheckResult">Check result computed during pre handle operation.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        private static async Task Fx2NlAsync(NLHandler nlHandler, Fx2NLParameters fx2NLParameters, CheckResult preHandleCheckResult,  LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            var result = await nlHandler.Fx2NLAsync(preHandleCheckResult, fx2NLParameters, cancellationToken).ConfigureAwait(false);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, result);
        }

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

            var nlHandler = await operationContext.GetNLHandler(fx2NlRequestParams.TextDocument.Uri, _nLHandlerFactory, fx2NlRequestParams, cancellationToken).ConfigureAwait(false);
            _ = nlHandler ?? throw new ArgumentNullException(nameof(nlHandler));
            if (!nlHandler.SupportsFx2NL)
            {
                throw new NotSupportedException("Fx2Nl is not supported");
            }
        
            var (fx2NLParameters, preHandleCheckResult) = await PreHandleFx2NlAsync(fx2NlRequestParams, nlHandler, operationContext, cancellationToken).ConfigureAwait(false);
            if (preHandleCheckResult == null)
            {
                return;
            }

            await Fx2NlAsync(nlHandler, fx2NLParameters, preHandleCheckResult, operationContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
