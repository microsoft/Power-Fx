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
        private NLHandler _nlHandler;

        public BackwardsCompatibleFx2NlLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        protected override async Task<bool> PreHandleFx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            // Existing SDK relies on GetNLHandler to get the handler for Nl2Fx operation.
            // This is a legacy handler and not to be confused with the new handler pattern.
            _nlHandler = _nLHandlerFactory.GetNLHandler(operationContext.GetScope(_fx2NlRequestParams.TextDocument.Uri), _fx2NlRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Fx2Nl");
            if (!_nlHandler.SupportsFx2NL)
            {
                throw new NotSupportedException("Fx2Nl is not supported");
            }

            return await base.PreHandleFx2NlAsync(operationContext, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<bool> Fx2NlAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            var result = await _nlHandler.Fx2NLAsync(_checkResultFromInputExpresion, _fx2NlParameters, cancellationToken).ConfigureAwait(false);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, result);
            return true;
        }
    }
}
