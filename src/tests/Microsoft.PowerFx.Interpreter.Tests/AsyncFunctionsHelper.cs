// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Tests
{
    // Helper for implementing Aysnc/WaitFor functions that test parallelism.
    internal abstract class AsyncFunctionHelperBase
    {
        private readonly string _functionName;

        public AsyncFunctionHelperBase(string functionName)
        {
            _functionName = functionName;
        }

        private readonly List<TaskCompletionSource<int>> _list = new List<TaskCompletionSource<int>>();

        public TaskCompletionSource<int> Get(int i)
        {
            lock (_list)
            {
                while (_list.Count <= i)
                {
                    _list.Append(new TaskCompletionSource<int>());
                }
            }

            return _list[i];
        }

        private int GetMax()
        {
            return _list.Count;
        }

        public async Task WaitFor(int i)
        {
            var tsc = Get(i);
            await tsc.Task;
        }

        public void Complete(int i)
        {
            var tsc = Get(i);
            tsc.SetResult(i);
        }

        protected abstract Task OnWaitPolicyAsync(int x);
        
        public async Task<double> Worker(double d)
        {
            var result = await Worker(new FormulaValue[] { FormulaValue.New(d) }, CancellationToken.None);
            return ((NumberValue)result).Value;
        }

        public async Task<FormulaValue> Worker(FormulaValue[] args, CancellationToken cancel)
        {
            var i = (int)((NumberValue)args[0]).Value;

            // will create the task, singals to listened there is an outstanding call.
            Get(i);

            for (var x = 0; x < i; x++)
            {
                cancel.ThrowIfCancellationRequested();

                await OnWaitPolicyAsync(x);                
            }

            Complete(i);

            return args[0];
        }

        public TexlFunction GetFunction()
        {
            return new CustomAsyncTexlFunction(_functionName, DType.Number, DType.Number)
            {
                _impl = Worker
            };
        }
    }

    // WaitFor(N) hard fails if WaitFor(N-1) hasn't complete yet. 
    internal class WaitForFunctionsHelper : AsyncFunctionHelperBase
    {
        public WaitForFunctionsHelper()
            : base("WaitFor")
        {
        }

        protected override async Task OnWaitPolicyAsync(int x)
        {
            // Fail if any previous instance started before this one.
            var t = WaitFor(x);
            if (!t.IsCompleted)
            {
                throw new InvalidOperationException($"Task {x} has not completed. Bad parallelism");
            }
        }
    }

    // Async(N) returns N. Does not complete until Async(N-1) completes. 
    // Async(0) is completed.
    internal class AsyncFunctionsHelper : AsyncFunctionHelperBase
    {
        public AsyncFunctionsHelper()
            : base("Async")
        {
        }

        protected override async Task OnWaitPolicyAsync(int x)
        {
            // all previous instances must have finished before this completes.
            // But it's fine for them to start in paralle. 
            await WaitFor(x);            
        }
    }
}
