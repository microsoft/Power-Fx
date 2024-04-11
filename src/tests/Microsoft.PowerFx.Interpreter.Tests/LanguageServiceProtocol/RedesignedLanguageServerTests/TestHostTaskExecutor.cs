// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestHostTaskExecutor : IHostTaskExecutor
    {
        public async Task<TOutput> ExecuteTaskAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> task, TInput input, LanguageServerOperationContext operationContext, CancellationToken cancellationToken, TOutput defaultOutput = default)
        {
            // Simulate a delay in the task execution
            await Task.Delay(30, cancellationToken).ConfigureAwait(false);
            return await task(input).ConfigureAwait(false);
        }

        public async Task<TOutput> ExecuteTaskAsync<TOutput>(Func<Task<TOutput>> task, LanguageServerOperationContext operationContext, CancellationToken cancellationToken, TOutput defaultOutput = default)
        {
            // Simulate a delay in the task execution
            await Task.Delay(30, cancellationToken).ConfigureAwait(false);
            return await task().ConfigureAwait(false);
        }

        public async Task ExecuteTaskAsync(Action task, LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            // Simulate a delay in the task execution
            await Task.Delay(30, cancellationToken).ConfigureAwait(false);
            task();
        }
    }
}
