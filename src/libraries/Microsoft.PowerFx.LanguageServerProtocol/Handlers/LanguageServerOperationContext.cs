// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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

        /// <summary>
        /// Host Task Executor to run lsp tasks in host environment.
        /// </summary>
        public IHostTaskExecutor HostTaskExecutor { get; init; }
    }

    internal static class LanguageServerOperationContextExtensions
    {
        /// <summary>
        ///  A helper method to construct a check result for the given uri and expression.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <returns>CheckResult.</returns>
        public static CheckResult Check(this LanguageServerOperationContext context, string uri, string expression)
        {
            var scope = context.GetScope(uri);
            return scope?.Check(expression);
        }

        /// <summary>
        /// A helper method to suggest intellisense for the given expression.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <param name="cursorPosition">Cursor position in the expression.</param>
        /// <returns>Intellisense results.</returns>
        public static IIntellisenseResult Suggest(this LanguageServerOperationContext context, string uri, string expression, int cursorPosition)
        {
            var scope = context.GetScope(uri);
            return scope?.Suggest(expression, cursorPosition);
        }

        /// <summary>
        /// A helper method to convert the expression to display format.
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="uri">Uri to use for check result creation.</param>
        /// <param name="expression">Expression to create check result for.</param>
        /// <returns>Converted expression.</returns>
        public static string ConvertToDisplay(this LanguageServerOperationContext context, string uri, string expression)
        {
            var scope = context.GetScope(uri);
            return scope?.ConvertToDisplay(expression);
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

        /// <summary>
        ///  A helper method to execute a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <typeparam name="TInput">Input Type.</typeparam>
        /// <typeparam name="TOutput">Output Type.</typeparam>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="input">Input to the task.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Output.</returns>
        public static async Task<TOutput> ExecuteHostTaskAsync<TInput, TOutput>(this LanguageServerOperationContext context, Func<TInput, Task<TOutput>> task, TInput input, CancellationToken cancellationToken)
        {
            if (context?.HostTaskExecutor == null)
            {
                return await task(input).ConfigureAwait(false);
            }

            return await context.HostTaskExecutor.ExecuteTaskAsync(task, input, context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///  A helper method to execute a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <typeparam name="TOutput">Output Type.</typeparam>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Output.</returns>
        public static async Task<TOutput> ExecuteHostTaskAsync<TOutput>(this LanguageServerOperationContext context, Func<Task<TOutput>> task,  CancellationToken cancellationToken)
        {
            if (context?.HostTaskExecutor == null)
            {
                return await task().ConfigureAwait(false);
            }

            return await context.HostTaskExecutor.ExecuteTaskAsync(task, context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///  A helper method to execute a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <param name="context">Language Server Operation Context.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        public static async Task ExecuteHostTaskAsync(this LanguageServerOperationContext context, Action task, CancellationToken cancellationToken)
        {
            if (context?.HostTaskExecutor == null)
            {
                task();
                return;
            }

            await context.HostTaskExecutor.ExecuteTaskAsync(task, context, cancellationToken).ConfigureAwait(false);
        }
    }
}
