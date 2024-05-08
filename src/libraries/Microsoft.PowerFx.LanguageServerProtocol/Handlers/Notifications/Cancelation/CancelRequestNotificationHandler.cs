// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler to handle the $/cancelRequest notification.
    /// </summary>
    internal sealed class CancelRequestNotificationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => false;

        public string LspMethod => TextDocumentNames.CancelRequest;

        private readonly IHostCancelationHandler _hostCancelationHandler;

        public CancelRequestNotificationHandler(IHostCancelationHandler hostCancelationHandler)
        {
            _hostCancelationHandler = hostCancelationHandler;
        }

        public Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (_hostCancelationHandler == null 
                || !operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out CancelRequestParams cancelRequestParams)
                || string.IsNullOrWhiteSpace(cancelRequestParams.Id))
            {
                return Task.CompletedTask;
            }

            _hostCancelationHandler.CancelByRequestId(cancelRequestParams.Id);
            return Task.CompletedTask;
        }
    }
}
