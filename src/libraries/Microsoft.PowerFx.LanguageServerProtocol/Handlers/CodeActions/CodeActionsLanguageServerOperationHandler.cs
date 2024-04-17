﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using static Microsoft.PowerFx.LanguageServerProtocol.LanguageServer;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler for code actions.
    /// </summary>
    internal sealed class CodeActionsLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => TextDocumentNames.CodeAction;

        private readonly OnLogUnhandledExceptionHandler _onLogUnhandledExceptionHandler;

        public CodeActionsLanguageServerOperationHandler(OnLogUnhandledExceptionHandler onLogUnhandledExceptionHandler = null)
        {
            _onLogUnhandledExceptionHandler = onLogUnhandledExceptionHandler;
        }
         
        /// <summary>
        /// Compute quick fixes for the given expression.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="expression">Expression for which quick fixes need to be computed.</param>
        /// <param name="codeActionParams">Code Action Params.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Code Action Results.</returns>
        private Task<CodeActionResult[]> HandleQuickFixes(LanguageServerOperationContext operationContext, string expression, CodeActionParams codeActionParams, CancellationToken cancellationToken)
        {
            return operationContext.ExecuteHostTaskAsync(
            codeActionParams.TextDocument.Uri,
            (scope) =>
            {
                if (scope is EditorContextScope scopeQuickFix)
                {
                    return Task.FromResult(scopeQuickFix.SuggestFixes(expression, _onLogUnhandledExceptionHandler));
                }

                return Task.FromResult(Array.Empty<CodeActionResult>());
            }, cancellationToken,
            defaultOutput: Array.Empty<CodeActionResult>());
        }

        /// <summary>
        /// Handles the code actions operation.
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleCodeActionRequest: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");

            if (!operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out CodeActionParams codeActionParams))
            {
                return;
            }

            var documentUri = codeActionParams.TextDocument.Uri;
            var uri = new Uri(documentUri);
            var expression = LanguageServerHelper.ChooseExpression(codeActionParams, HttpUtility.ParseQueryString(uri.Query));
            if (expression == null)
            {
                operationContext.OutputBuilder.AddInvalidParamsError(operationContext.RequestId, "Failed to choose expression for code actions operation");
                return;
            }

            var codeActions = new Dictionary<string, CodeAction[]>();
            foreach (var codeActionKind in codeActionParams.Context.Only)
            {
                var results = new CodeActionResult[0];
                switch (codeActionKind)
                {
                    case CodeActionKind.QuickFix:
                        results = await HandleQuickFixes(operationContext, expression, codeActionParams, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        // No action.
                        return;
                }

                var items = new List<CodeAction>();
                foreach (var item in results)
                {
                    var range = item.Range ?? codeActionParams.Range;
                    items.Add(new CodeAction()
                    {
                        Title = item.Title,
                        Kind = codeActionKind,
                        Edit = new WorkspaceEdit
                        {
                            Changes = new Dictionary<string, TextEdit[]> { { documentUri, new[] { new TextEdit { Range = range, NewText = item.Text } } } }
                        },
                        ActionResultContext = item.ActionResultContext
                    });
                }
                    
                codeActions.Add(codeActionKind, items.ToArray());
            }

            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, codeActions);
        }
    }
}
