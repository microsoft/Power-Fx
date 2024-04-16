// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler to handle the GetCustomCapabilities request.
    /// This is backwards compatible as it used the NLHandlerFactory to get the NLHandler.
    /// This should ideally be replaced with the LSP initialization protocol.
    /// </summary>
    internal sealed class GetCustomCapabilitiesLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.GetCapabilities;

        private readonly INLHandlerFactory _nlHandlerFactory;

        public GetCustomCapabilitiesLanguageServerOperationHandler(INLHandlerFactory nlHandlerFactory)
        {
            _nlHandlerFactory = nlHandlerFactory;
        }

        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out CustomGetCapabilitiesParams customGetCapabilitiesParams))
            {
                operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new CustomGetCapabilitiesResult());
                return;
            }

            var nlHandler = operationContext.GetNLHandler(customGetCapabilitiesParams.TextDocument.Uri, _nlHandlerFactory, null);
            if (nlHandler == null)
            {
                operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new CustomGetCapabilitiesResult());
                return;
            }

            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new CustomGetCapabilitiesResult() { SupportsNL2Fx = nlHandler.SupportsNL2Fx, SupportsFx2NL = nlHandler.SupportsFx2NL });
        }
    }
}
