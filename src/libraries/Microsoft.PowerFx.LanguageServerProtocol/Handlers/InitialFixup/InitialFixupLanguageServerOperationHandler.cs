﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handles the initial fixup operation.
    /// </summary>
    public class InitialFixupLanguageServerOperationHandler : BaseLanguageServerOperationHandler
    {
        public override bool IsRequest => true;

        public override string LspMethod => CustomProtocolNames.InitialFixup;

        /// <summary>
        /// Handles the initial fixup operation.
        /// </summary>
        /// <param name="operationContext"> Language Server Operation Context. </param>
        /// <param name="cancellationToken"> Cancellation Token. </param>   
        public override async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleInitialFixupRequest: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out InitialFixupParams requestParams))
            {
                return;
            }

            var expression = await operationContext.ConvertToDisplayAsync(requestParams.TextDocument.Uri, requestParams.TextDocument.Text, cancellationToken).ConfigureAwait(false);

            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new TextDocumentItem()
            {
                Uri = requestParams.TextDocument.Uri,
                Text = expression
            });
        }
    }
}