// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler for the signature help operation.
    /// Currently, Language Server SDK delegates full signature help logic to the host.
    /// It only does validation of the input and then transforms the results to the LSP format.
    /// Those are not needed to be exposed as overridable methods/hooks.
    /// Therefore, there's only one HandleAsync method.
    /// </summary>
    public class SignatureHelpLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public string LspMethod => TextDocumentNames.SignatureHelp;

        public bool IsRequest => true;

        /// <summary>
        /// Provides the signature help for the given expression.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="uri">Document Uri.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="cursorPosition">Cursor Position.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Suggestions and Signatures.</returns>
        private Task<IIntellisenseResult> SuggestAsync(LanguageServerOperationContext operationContext, string uri, string expression, int cursorPosition, CancellationToken cancellationToken)
        {
            return operationContext.ExecuteHostTaskAsync(
            () => Task.FromResult(operationContext.Suggest(uri, expression, cursorPosition)),
            cancellationToken);
        }

        /// <summary>
        /// Handles the signature help operation and computes the signature help.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleSignatureHelpRequest: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out SignatureHelpParams signatureHelpParams))
            {
                return;
            }

            var documentUri = new Uri(signatureHelpParams.TextDocument.Uri);
            var expression = LanguageServerHelper.ChooseExpression(signatureHelpParams, HttpUtility.ParseQueryString(documentUri.Query));
            if (expression == null)
            {
                operationContext.OutputBuilder.AddInvalidParamsError(operationContext.RequestId, "Failed to choose expression for singature help operation");
                return;
            }

            var cursorPosition = PositionRangeHelper.GetPosition(expression, signatureHelpParams.Position.Line, signatureHelpParams.Position.Character);
            var scope = operationContext.GetScope(signatureHelpParams.TextDocument.Uri);
            var results = await SuggestAsync(operationContext, signatureHelpParams.TextDocument.Uri, expression, cursorPosition, cancellationToken).ConfigureAwait(false);
            if (results == null || results == default)
            {
                operationContext.OutputBuilder.AddInternalError(operationContext.RequestId, "Failed to get suggestions for signature help operation");
                return;
            }

            var signatureHelp = new SignatureHelp(results.SignatureHelp);
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, signatureHelp);
        }
    }
}
