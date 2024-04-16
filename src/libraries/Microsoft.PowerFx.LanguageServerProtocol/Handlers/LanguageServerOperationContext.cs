// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

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
        internal ILanguageServerLogger Logger { get; init; }

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
        internal LanguageServerOutputBuilder OutputBuilder { get; init; }

        /// <summary>
        /// Host Task Executor to run tasks in host environment.
        /// </summary>
        internal IHostTaskExecutor HostTaskExecutor { get; init; }

        private IPowerFxScope _scope = null;

        /// <summary>
        /// Gets or creates the scope for the operation.
        /// </summary>
        /// <param name="uri">Uri to use for scope creation.</param>
        /// <returns>Scope.</returns>
        private IPowerFxScope GetScope(string uri)
        {
            return _scope ??= _scopeFactory.GetOrCreateInstance(uri);
        }

        /// <summary>
        /// Get the NLHandler for the given uri and factory.
        /// </summary>
        /// <param name="uri">Uri to use for scope creation.</param>
        /// <param name="factory">Nl Handler Factory.</param>
        /// <param name="nLParams">Ml Params.</param>
        /// <returns>NlHandler Instance.</returns>
        internal NLHandler GetNLHandler(string uri, INLHandlerFactory factory, BaseNLParams nLParams = null)
        {
            return factory.GetNLHandler(GetScope(uri), nLParams);
        }

        /// <summary>
        ///  A helper method to execute a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <typeparam name="TOutput">Output Type.</typeparam>
        /// <param name="uri">Uri to use for scope creation.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="defaultOutput"> Default Output if task is canceled by the host.</param>
        /// <returns>Output.</returns>
        internal async Task<TOutput> ExecuteHostTaskAsync<TOutput>(string uri, Func<IPowerFxScope, Task<TOutput>> task, CancellationToken cancellationToken, TOutput defaultOutput = default)
        {
            if (HostTaskExecutor == null)
            {
                return await task(GetScope(uri)).ConfigureAwait(false);
            }

            Task<TOutput> WrappedTask() => task(GetScope(uri));
            return await HostTaskExecutor.ExecuteTaskAsync(WrappedTask, this, cancellationToken, defaultOutput).ConfigureAwait(false);
        }

        /// <summary>
        ///  A helper method to execute a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <param name="uri">Uri to use for scope creation.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        internal async Task ExecuteHostTaskAsync(string uri, Action<IPowerFxScope> task, CancellationToken cancellationToken)
        {
            if (HostTaskExecutor == null)
            {
                task(GetScope(uri));
                return;
            }

            await HostTaskExecutor.ExecuteTaskAsync(
            () =>
            {
                task(GetScope(uri));
                return Task.FromResult<object>(null);
            }, this, 
            cancellationToken).ConfigureAwait(false);
        }
    }

    internal static class LanguageServerOperationContextExtensions
    {
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
