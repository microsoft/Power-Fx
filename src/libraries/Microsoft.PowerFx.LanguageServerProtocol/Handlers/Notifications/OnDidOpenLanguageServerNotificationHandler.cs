// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler to handle the DidOpen notification from the client.
    /// </summary>
    internal sealed class OnDidOpenLanguageServerNotificationHandler : ILanguageServerOperationHandler
    {
        public string LspMethod => TextDocumentNames.DidOpen;

        public bool IsRequest => false;

        /// <summary>
        /// Handles the DidOpen notification from the client.
        /// </summary>
        /// <param name="operationContext"> Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleDidOpenNotification: paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out DidOpenTextDocumentParams didOpenParams))
            {
                return;
            }

            await operationContext.ExecuteHostTaskAsync(
            didOpenParams.TextDocument.Uri,
            (scope) => 
            {
                var dURI = new Uri(didOpenParams.TextDocument.Uri);
                var query = HttpUtility.ParseQueryString(dURI.Query);
                
                if (!(query.Get("lspMode")?.ToString() is string lspModeStr &&
                Enum.TryParse<LSPExpressionMode>(lspModeStr, true, out var lspMode)))
                {
                    lspMode = LSPExpressionMode.Default;
                }

                if (lspMode == LSPExpressionMode.UserDefinedFunction)
                {
                    var udfCheck = scope?.CheckUserDefinedFunctions(didOpenParams.TextDocument.Text);
                    operationContext.OutputBuilder.WriteDiagnosticsNotification(didOpenParams.TextDocument.Uri, didOpenParams.TextDocument.Text, udfCheck.Errors.ToArray());
                    return;
                }

                var checkResult = scope?.Check(didOpenParams.TextDocument.Text);
                if (checkResult == null)
                {
                    return;
                }

                operationContext.OutputBuilder.WriteDiagnosticsNotification(didOpenParams.TextDocument.Uri, didOpenParams.TextDocument.Text, checkResult.Errors.ToArray());

                operationContext.OutputBuilder.WriteTokensNotification(didOpenParams.TextDocument.Uri, checkResult);

                operationContext.OutputBuilder.WriteExpressionTypeNotification(didOpenParams.TextDocument.Uri, checkResult);
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
