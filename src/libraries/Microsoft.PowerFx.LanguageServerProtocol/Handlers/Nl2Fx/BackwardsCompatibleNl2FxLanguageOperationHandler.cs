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
        private NLHandler _nlHandler;

        public BackwardsCompatibleNl2FxLanguageServerOperationHandler(INLHandlerFactory nLHandlerFactory)
        {
            _nLHandlerFactory = nLHandlerFactory;
        }

        protected override async Task<bool> PreHandleNl2FxAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            // Existing SDK relies on GetNLHandler to get the handler for Nl2Fx operation.
            // This is a legacy handler and not to be confused with the new handler pattern.
            _nlHandler = _nLHandlerFactory.GetNLHandler(operationContext.GetScope(_nl2FxRequestParams.TextDocument.Uri), _nl2FxRequestParams) ?? throw new NullReferenceException("No suitable handler found to handle Nl2Fx");
            if (!_nlHandler.SupportsNL2Fx)
            {
                throw new NotSupportedException("Nl2fx is not supported");
            }

            return await base.PreHandleNl2FxAsync(operationContext, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<bool> Nl2FxAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {   
            _nl2FxResult = await _nlHandler.NL2FxAsync(_nl2FxParameters, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
