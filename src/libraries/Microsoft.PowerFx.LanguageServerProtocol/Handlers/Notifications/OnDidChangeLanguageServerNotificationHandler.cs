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
    /// Handler to handle the DidChangeTextDocument notification.
    /// </summary>
    internal sealed class OnDidChangeLanguageServerNotificationHandler : ILanguageServerOperationHandler
    {
        public string LspMethod => TextDocumentNames.DidChange;

        public bool IsRequest => false;

        private readonly LanguageServer.NotifyDidChange _notifyDidChange;

        public OnDidChangeLanguageServerNotificationHandler(LanguageServer.NotifyDidChange notifyDidChange)
        {
            _notifyDidChange = notifyDidChange;
        }

        /// <summary>
        /// Handles the DidChangeTextDocument notification.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleDidChangeNotification: paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out DidChangeTextDocumentParams didChangeParams))
            {
                return;
            }

            if (didChangeParams.ContentChanges.Length != 1)
            {
                return;
            }

            var documentUri = didChangeParams.TextDocument.Uri;

            var expression = didChangeParams.ContentChanges[0].Text;
            await operationContext.ExecuteHostTaskAsync(
            documentUri,
            (scope) =>
            {
                _notifyDidChange?.Invoke(didChangeParams);
                var dURI = new Uri(documentUri);
                var query = HttpUtility.ParseQueryString(dURI.Query);
                if (!(query.Get("lspMode")?.ToString() is string lspModeStr &&
                Enum.TryParse<LSPExpressionMode>(lspModeStr, true, out var lspMode)))
                {
                    lspMode = LSPExpressionMode.Default;
                }

                if (lspMode == LSPExpressionMode.UserDefinedFunction)
                {
                    var udfCheck = scope?.CheckUserDefinedFunctions(expression);
                    operationContext.OutputBuilder.WriteDiagnosticsNotification(didChangeParams.TextDocument.Uri, expression, udfCheck.Errors.ToArray());
                    return;
                }

                var checkResult = scope?.Check(expression);

                operationContext.OutputBuilder.WriteDiagnosticsNotification(didChangeParams.TextDocument.Uri, expression, checkResult.Errors.ToArray());

                operationContext.OutputBuilder.WriteTokensNotification(didChangeParams.TextDocument.Uri, checkResult);

                operationContext.OutputBuilder.WriteExpressionTypeNotification(didChangeParams.TextDocument.Uri, checkResult);
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
