// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    ///  Handler for CommandExecuted operation.
    /// </summary>
    internal sealed class CommandExecutedLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => CustomProtocolNames.CommandExecuted;

        /// <summary>
        /// Hook to handle the CodeActionApplied operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="commandExecutedParams">Command Executed Params.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        private async Task HandleCodeActionApplied(LanguageServerOperationContext operationContext, CommandExecutedParams commandExecutedParams, CancellationToken cancellationToken)
        {
            var codeActionResult = JsonRpcHelper.Deserialize<CodeAction>(commandExecutedParams.Argument);
            if (codeActionResult.ActionResultContext == null)
            {
                operationContext.OutputBuilder.AddProperyValueRequiredError(operationContext.RequestId, $"{nameof(CodeAction.ActionResultContext)} is null or empty.");
                return;
            }

            await operationContext.ExecuteHostTaskAsync(
            commandExecutedParams.TextDocument.Uri,
            (scope) =>
            {
                if (scope is EditorContextScope scopeQuickFix)
                {
                    scopeQuickFix.OnCommandExecuted(codeActionResult);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the CommandExecuted operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleCommandExecutedRequest: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");
            if (!TryParseAndValidateParams(operationContext, out var commandExecutedParams))
            {
                return;
            }

            switch (commandExecutedParams.Command)
            {
                case CommandName.CodeActionApplied:
                    await HandleCodeActionApplied(operationContext, commandExecutedParams, cancellationToken).ConfigureAwait(false);
                    return;
                default:
                    operationContext.OutputBuilder.AddInvalidRequestError(operationContext.RequestId, $"{commandExecutedParams.Command} is not supported.");
                    return;
            }
        }

        /// <summary>
        /// A helper method to parse and validate the params for CommandExecuted.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="commandExecutedParams">Reference to hold the commond executed params.</param>
        /// <returns>True if parsing and validation were successful or false otherwise.</returns>
        private static bool TryParseAndValidateParams(LanguageServerOperationContext operationContext, out CommandExecutedParams commandExecutedParams)
        {
            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out commandExecutedParams))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(commandExecutedParams?.Argument))
            {
                operationContext.OutputBuilder.AddProperyValueRequiredError(operationContext.RequestId, $"{nameof(CommandExecutedParams.Argument)} is null or empty.");
                return false;
            }

            return true;
        }
    }
}
