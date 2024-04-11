// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// A task executor that can be used to execute LSP tasks in the host environment.
    /// </summary>
    public interface IHostTaskExecutor
    {
        /// <summary>
        ///  Executes a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <typeparam name="TOutput">Output Type.</typeparam>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="defaultOutput">Default Output to return if the provided task is canceled by host.</param>
        /// <returns>Output.</returns>
        Task<TOutput> ExecuteTaskAsync<TOutput>(Func<Task<TOutput>> task, LanguageServerOperationContext operationContext, CancellationToken cancellationToken, TOutput defaultOutput = default);

        /// <summary>
        ///  Executes a task in host environment if host task executor is available.
        ///  This is critical to trust between LSP and its hosts.
        ///  LSP should correctly use this and wrap particulat steps that need to run in host using these methods. 
        /// </summary>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <param name="task">Task to run inside host.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        Task ExecuteTaskAsync(Action task, LanguageServerOperationContext operationContext, CancellationToken cancellationToken);
    }
}
