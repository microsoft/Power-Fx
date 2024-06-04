// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Tests
{
    // Helper for implementing Aysnc/WaitFor functions that test parallelism.
    internal abstract class AsyncFunctionHelperBase
    {
        private readonly string _functionName;

        private readonly AsyncVerify _verify;

        public AsyncFunctionHelperBase(AsyncVerify verify, string functionName)
        {
            _functionName = functionName;
            _verify = verify;
        }

        private readonly List<TaskCompletionSource<int>> _list = new List<TaskCompletionSource<int>>();

        private TaskCompletionSource<int> Get(int i)
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

            if (_verify != null)
            {
                await _verify.BeginAsyncCall(i);
            }

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
}
