// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A context for a language server operation with information that might be needed when handling the operation.
    /// It also has helper methods to simplify the operation handling.
    /// Lifetime of this context is per request.
    /// </summary>
    public class LanguageServerOperationContext
    {
        /// <summary>
        /// A factory to create a scope for the operation.
        /// </summary>
        private readonly IPowerFxScopeFactory _scopeFactory;

        public LanguageServerOperationContext(IPowerFxScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Language Server Method Identifier for which this context is created.
        /// </summary>
        public string LspMethod { get; init; }

        /// <summary>
        /// Language Server Logger.
        /// </summary>
        public ILanguageServerLogger Logger { get; init; }

        /// <summary>
        /// ID of the request. This won't be relevant for notification handlers.
        /// </summary>
        public string RequestId { get; init; }
        
        /// <summary>
        /// Raw input of the operation. Usually this is passed from the client.
        /// </summary>
        public string RawOperationInput { get; init; }

        /// <summary>
        /// A builder to create the output of the operation.
        /// No handlers are expected to return a value. They should use this builder to write the response.
        /// A handler can write multiple responses and the builder will take care of the correct format.
        /// </summary>
        public LanguageServerOutputBuilder OutputBuilder { get; init; }

        private IPowerFxScope _scope = null;

        /// <summary>
        /// Gets or creates the scope for the operation.
        /// </summary>
        /// <param name="uri">Uri to use for scope creation.</param>
        /// <returns>Scope.</returns>
        public IPowerFxScope GetScope(string uri)
        {
            return _scope ??= _scopeFactory.GetOrCreateInstance(uri);
        }
    }

    internal static class LanguageServerOperationContextExtensions
    {
        /// <summary>
        ///  A helper method to construct a check result for the given uri and expression.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>CheckResult.</returns>
        public static async Task<CheckResult> CheckAsync(this LanguageServerOperationContext context, string uri, string expression, CancellationToken cancellationToken)
        {
            var scope = context.GetScope(uri);

            // With the new SDK we support async scope which asynchronously creates the check result and run other operations
            // For consumers not adapting this new SDK directly, they would still be using the old synchronous scope.
            // Async scope extends the synchronous scope.
            // If the scope is async, we should await the check operation.
            // If the scope is not async, we should run the check operation synchronously.
            return scope is IAsyncPowerFxScope asyncScope ?
                   await asyncScope.CheckAsync(expression, cancellationToken).ConfigureAwait(false) :
                   scope?.Check(expression);
        }

        /// <summary>
        /// A helper method to suggest intellisense for the given expression.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <param name="cursorPosition">Cursor position in the expression.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Intellisense results.</returns>
        public static async Task<IIntellisenseResult> SuggestAsync(this LanguageServerOperationContext context, string uri, string expression, int cursorPosition, CancellationToken cancellationToken)
        {
            var scope = context.GetScope(uri);

            // With the new SDK we support async scope which asynchronously runs suggest and other operations
            // For consumers not adapting this new SDK directly, they would still be using the old synchronous scope.
            // Async scope extends the synchronous scope.
            // If the scope is async, we should await the suggest operation.
            // If the scope is not async, we should run the suggest operation synchronously.
            return scope is IAsyncPowerFxScope asyncScope ?
                   await asyncScope.SuggestAsync(expression, cursorPosition, cancellationToken).ConfigureAwait(false) :
                   scope?.Suggest(expression, cursorPosition);
        }

        /// <summary>
        /// A helper method to convert the expression to display format.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Converted expression.</returns>
        public static async Task<string> ConvertToDisplayAsync(this LanguageServerOperationContext context, string uri, string expression, CancellationToken cancellationToken)
        {
            var scope = context.GetScope(uri);

            return scope is IAsyncPowerFxScope asyncScope ?
                   await asyncScope.ConvertToDisplayAsync(expression, cancellationToken).ConfigureAwait(false) :
                   scope?.ConvertToDisplay(expression);
        }

        /// <summary>
        /// A helper method to help parse the raw input of the operation and add an error response if parsing fails.
        /// </summary>
        /// <typeparam name="T">Deserialized and parsed type.</typeparam>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="parsedParams">Reference to hold the deserialized/parsed params.</param>
        /// <returns>True if parsing was successful or false otherwise.</returns>
        public static bool TryParseParamsAndAddErrorResponseIfNeeded<T>(this LanguageServerOperationContext context, out T parsedParams)
        {
            if (!LanguageServerHelper.TryParseParams(context.RawOperationInput, out parsedParams))
            {
                context.OutputBuilder.AddParseError(context.RequestId, $"Cannot parse the params for method: ${context.LspMethod}");
                return false;
            }

            return true;
        }
    }
}
