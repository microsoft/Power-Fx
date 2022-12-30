// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // This helper verifies we're actually awaiting results and not just
    // doing a blocking .Result. 
    // The async functions will call TestAsyncCall()
    internal class AsyncVerify
    {
        private readonly Dictionary<int, TaskCompletionSource<int>> _dict = new Dictionary<int, TaskCompletionSource<int>>();

        public bool HasOutanding => _dict.Count > 0;

        // Called by Async() test functions to mark that there's an outstanding call.
        // This will return a non-completed task. Await will will properly bubble back up,
        // whereas .Result will hang. 
        internal async Task BeginAsyncCall(int idx)
        {
            // If this hangs, then some thread is stuck in .Result.

            var task = BeginAsyncCallWorker(idx);
            var timer = Task.Delay(TimeSpan.FromSeconds(10));

            if (await Task.WhenAny(task, timer) == task)
            {
                return; // Success
            }

            if (Debugger.IsAttached)
            {
                // If we get here, some other thread has called .Result instead of await. 
                Debugger.Break();
            }

            await task;
        }

        private Task BeginAsyncCallWorker(int idx)
        {
            // Tasks can get called in parallel. 
            lock (_dict)
            {
                var tsc = new TaskCompletionSource<int>();
                _dict[idx] = tsc;

                return tsc.Task;
            }
        }

        // Called at the top of the callstack to pair with TestAsyncCall.
        public void Complete()
        {
            TaskCompletionSource<int> tsc;
            lock (_dict)
            {
                var kv = _dict.First();
                _dict.Remove(kv.Key);
                tsc = kv.Value;
            }

            tsc.SetResult(1); // signals eval to keep running.
        }

        public async Task<FormulaValue> EvalAsync(RecalcEngine engine, string expr, InternalSetup setup)
        {
            var rtConfig = new RuntimeConfig();

            if (setup.TimeZoneInfo != null)
            {
                rtConfig.AddService(setup.TimeZoneInfo);
            }

            var task = engine.EvalAsync(expr, CancellationToken.None, options: setup.Flags.ToParserOptions(), runtimeConfig: rtConfig);

            var i = 0;
            while (HasOutanding)
            {
                i++;

                // If there was a .Result, it would wait for results and complete. 
                // Eval can't be done yet because it's in the middle of an Async call 
                // which is blocked on the Complete() call below.
                Assert.False(task.IsCompleted);
                Complete();

                await Task.Yield();
            }

            var result = await task;
            return result;
        }
    }
}
