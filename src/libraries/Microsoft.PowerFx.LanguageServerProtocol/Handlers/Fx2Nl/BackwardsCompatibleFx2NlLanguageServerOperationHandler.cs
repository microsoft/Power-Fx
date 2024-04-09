// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A handler that makes newly structured Fx2NL operations backwards compatible with the existing Language Server SDK.
    /// Sealed class because it is not meant to be inherited and only used for maintaining backwards compatibility.
    /// </summary>
    internal sealed class BackwardsCompatibleFx2NlLanguageServerOperationHandler : BaseFx2NlLanguageServerOperationHandler
    {
        private readonly INLHandlerFactory _nLHandlerFactory;

        public BackwardsCompatibleFx2NlLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        protected override async Task<Fx2NlHandleContext> PreHandleFx2NlAsync(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext, CancellationToken cancellationToken)
        {
            // Existing SDK relies on GetNLHandler to get the handler for Nl2Fx operation.
            // This is a legacy handler and not to be confused with the new handler pattern.
            var nlHandler = _nLHandlerFactory?.GetNLHandler(operationContext.GetScope(handleContext.fx2NlRequestParams.TextDocument.Uri), handleContext.fx2NlRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Fx2Nl");
            if (!nlHandler.SupportsFx2NL)
            {
                throw new NotSupportedException("Fx2Nl is not supported");
            }

            var basePreHandleResult = await base.PreHandleFx2NlAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
            return handleContext with { preHandleResult = new BackwardsCompatibleFx2NlPreHandleResult(nlHandler, basePreHandleResult.preHandleResult) };
        }

        protected override async Task<Fx2NlHandleContext> Fx2NlAsync(LanguageServerOperationContext operationContext, Fx2NlHandleContext handleContext, CancellationToken cancellationToken)
        { 
            var result = await (handleContext.preHandleResult as BackwardsCompatibleFx2NlPreHandleResult).nlHandler.Fx2NLAsync(handleContext.preHandleResult.checkResult, handleContext.preHandleResult.parameters, cancellationToken).ConfigureAwait(false);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, result);
            return handleContext;
        }
    }
}
