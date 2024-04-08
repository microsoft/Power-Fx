// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A handler that makes newly structured Nl2Fx operations backwards compatible with the existing Language Server SDK.
    /// Sealed class because it is not meant to be inherited and only used for maintaining backwards compatibility.
    /// </summary>
    internal sealed class BackwardsCompatibleNl2FxLanguageServerOperationHandler : BaseNl2FxLanguageServerOperationHandler
    {
        private readonly INLHandlerFactory _nLHandlerFactory;

        public BackwardsCompatibleNl2FxLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        protected override async Task<Nl2FxHandleContext> PreHandleNl2FxAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext, CancellationToken cancellationToken)
        {
            // Existing SDK relies on GetNLHandler to get the handler for Nl2Fx operation.
            // This is a legacy handler and not to be confused with the new handler pattern.
            var nlHandler = _nLHandlerFactory?.GetNLHandler(operationContext.GetScope(handleContext.nl2FxRequestParams.TextDocument.Uri), handleContext.nl2FxRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Nl2Fx");
            if (!nlHandler.SupportsNL2Fx)
            {
                throw new NotSupportedException("Nl2fx is not supported");
            }

            handleContext = await base.PreHandleNl2FxAsync(operationContext, handleContext, cancellationToken).ConfigureAwait(false);
            return handleContext with { preHandleResult = new BackwardsCompatibleNl2FxPreHandleResult(nlHandler, handleContext.preHandleResult) };
        }

        protected override async Task<Nl2FxHandleContext> Nl2FxAsync(LanguageServerOperationContext operationContext, Nl2FxHandleContext handleContext, CancellationToken cancellationToken)
        {   
            var nl2FxResult = await (handleContext.preHandleResult as BackwardsCompatibleNl2FxPreHandleResult).nlHandler.NL2FxAsync(handleContext.preHandleResult.parameters, cancellationToken).ConfigureAwait(false);
            return handleContext with { nl2FxResult = new Nl2FxResult(nl2FxResult) };
        }
    }
}
