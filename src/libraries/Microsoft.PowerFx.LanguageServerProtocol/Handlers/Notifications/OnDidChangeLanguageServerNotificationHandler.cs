// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler to handle the DidChangeTextDocument notification.
    /// </summary>
    public class OnDidChangeLanguageServerNotificationHandler : BaseLanguageServerOperationHandler
    {
        public override string LspMethod => TextDocumentNames.DidChange;

        public override bool IsRequest => false;

        /// <summary>
        /// An overridable hook that lets hosts run custom logic when a document changes.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="didChangeTextDocumentParams">Notification Params.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        protected virtual async Task OnDidChange(LanguageServerOperationContext operationContext, DidChangeTextDocumentParams didChangeTextDocumentParams, CancellationToken cancellationToken)
        {
            return;
        }

        /// <summary>
        /// Handles the DidChangeTextDocument notification.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public override async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleDidChangeNotification: paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded<DidChangeTextDocumentParams>(out var didChangeParams))
            {
                return;
            }

            if (didChangeParams.ContentChanges.Length != 1)
            { 
                return;
            }

            await OnDidChange(operationContext, didChangeParams, cancellationToken).ConfigureAwait(false);
            var documentUri = didChangeParams.TextDocument.Uri;

            var expression = didChangeParams.ContentChanges[0].Text;
            var checkResult = await operationContext.CheckAsync(documentUri, expression, cancellationToken).ConfigureAwait(false);
            if (checkResult == null)
            {
                return;
            }

            operationContext.OutputBuilder.WriteDiagnosticsNotification(didChangeParams.TextDocument.Uri, expression, checkResult.Errors.ToArray());

            operationContext.OutputBuilder.WriteTokensNotification(didChangeParams.TextDocument.Uri, checkResult);

            operationContext.OutputBuilder.WriteExpressionTypeNotification(didChangeParams.TextDocument.Uri, checkResult);
        }
    }
}
