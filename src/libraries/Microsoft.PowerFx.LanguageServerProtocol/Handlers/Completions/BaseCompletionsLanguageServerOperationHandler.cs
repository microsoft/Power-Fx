// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Handler for the completions operation.
    /// Currently, Language Server SDK delegates full completion logic to the host.
    /// It only does validation of the input and then transforms the results to the LSP format.
    /// Those are not needed to be exposed as overridable methods/hooks.
    /// Therefore, there's only one HandleAsync method.
    /// </summary>
    public class BaseCompletionsLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        public bool IsRequest => true;

        public string LspMethod => TextDocumentNames.Completion;

        /// <summary>
        /// Provides the suggestions for the given expression.
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
        /// Handles the completion operation and computes the completions.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public async Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            operationContext.Logger?.LogInformation($"[PFX] HandleCompletionRequest: id={operationContext.RequestId ?? "<null>"}, paramsJson={operationContext.RawOperationInput ?? "<null>"}");
            if (!TryParseAndValidateParams(operationContext, out var completionParams))
            {
                operationContext.Logger?.LogError($"[PFX] HandleCompletionRequest: ParseError");
                return;
            }

            var documentUri = new Uri(completionParams.TextDocument.Uri);
            var expression = LanguageServerHelper.ChooseExpression(completionParams, HttpUtility.ParseQueryString(documentUri.Query));
            if (expression == null)
            {
                operationContext.Logger?.LogError($"[PFX] HandleCompletionRequest: InvalidParams, expression is null");
                operationContext.OutputBuilder.AddInvalidParamsError(operationContext.RequestId, "Failed to choose expression for completions operation");
                return;
            }

            var cursorPosition = PositionRangeHelper.GetPosition(expression, completionParams.Position.Line, completionParams.Position.Character);

            operationContext.Logger?.LogInformation($"[PFX] HandleCompletionRequest: calling Suggest...");
            var results = await SuggestAsync(operationContext, completionParams.TextDocument.Uri, expression, cursorPosition, cancellationToken).ConfigureAwait(false);

            // Note: there are huge number of suggestions in initial requests
            // Including each of them in the log string is very expensive
            // Avoid this if possible
            operationContext.Logger?.LogInformation($"[PFX] HandleCompletionRequest: Suggest results: Count:{results.Suggestions.Count()}, Suggestions:{string.Join(", ", results.Suggestions.Select(s => $@"[{s.Kind}]: '{s.DisplayText.Text}'"))}");

            var precedingCharacter = cursorPosition != 0 ? expression[cursorPosition - 1] : '\0';
            operationContext.OutputBuilder.AddSuccessResponse(operationContext.RequestId, new
            {
                items = results.Suggestions.Select((item, index) => new CompletionItem()
                {
                    Label = item.DisplayText.Text,
                    Detail = item.FunctionParameterDescription,
                    Documentation = item.Definition,
                    Kind = GetCompletionItemKind(item.Kind),

                    // The order of the results should be preserved.  To do that, we embed the index
                    // into the sort text, which clients may sort lexicographically.
                    SortText = index.ToString("D3", CultureInfo.InvariantCulture),

                    // If the current position is in front of a single quote and the completion result starts with a single quote,
                    // we don't want to make it harder on the end user by inserting an extra single quote.
                    InsertText = item.DisplayText.Text is { } label && TexlLexer.IsIdentDelimiter(label[0]) && precedingCharacter == TexlLexer.IdentifierDelimiter ? label.Substring(1) : item.DisplayText.Text
                }),
                isIncomplete = false
            });
        }

        /// <summary>
        /// Attempts to parse and validate the completion params.
        /// </summary>
        /// <param name="operationContext">Operation Context.</param>
        /// <param name="completionParams">Reference to capture successfully parsed completion params.</param>
        /// <returns>True if parsing and validation was successful or false otherewise.</returns>
        private static bool TryParseAndValidateParams(LanguageServerOperationContext operationContext, out CompletionParams completionParams)
        {
            return operationContext.TryParseParamsAndAddErrorResponseIfNeeded(out completionParams);
        }

        /// <summary>
        /// Maps the suggestion kind to the completion item kind.
        /// </summary>
        /// <param name="kind">Suggestion Kind.</param>
        /// <returns>Mapped Completion Kind.</returns>
        protected static CompletionItemKind GetCompletionItemKind(SuggestionKind kind)
        {
            switch (kind)
            {
                case SuggestionKind.Function:
                    return CompletionItemKind.Method;
                case SuggestionKind.KeyWord:
                    return CompletionItemKind.Keyword;
                case SuggestionKind.Global:
                    return CompletionItemKind.Variable;
                case SuggestionKind.Field:
                    return CompletionItemKind.Field;
                case SuggestionKind.Alias:
                    return CompletionItemKind.Variable;
                case SuggestionKind.Enum:
                    return CompletionItemKind.Enum;
                case SuggestionKind.BinaryOperator:
                    return CompletionItemKind.Operator;
                case SuggestionKind.Local:
                    return CompletionItemKind.Variable;
                case SuggestionKind.ServiceFunctionOption:
                    return CompletionItemKind.Method;
                case SuggestionKind.Service:
                    return CompletionItemKind.Module;
                case SuggestionKind.ScopeVariable:
                    return CompletionItemKind.Variable;
                default:
                    return CompletionItemKind.Text;
            }
        }
    }
}
