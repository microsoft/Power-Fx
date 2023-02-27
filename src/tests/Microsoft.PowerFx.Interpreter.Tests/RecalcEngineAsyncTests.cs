// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Tests
{
    // Test async eval features. 
    public class RecalcEngineAsyncTests : PowerFxTest
    {
        // Intentionally trivial async function that runs synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async Task<FormulaValue> Worker(FormulaValue[] args, CancellationToken cancel)
        {
            var n = (NumberValue)args[0];

            var result = FormulaValue.New(n.Value * 2);
            return result;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        // Call a single async function 
        [Fact]
        public async Task SimpleAsync()
        {
            var func = new CustomAsyncTexlFunction("CustomAsync", DType.Number, DType.Number)
            {
                _impl = Worker
            };

            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var result = await engine.EvalAsync("CustomAsync(3)", cts.Token);
            Assert.Equal(6.0, result.ToObject());
        }

        // Call an expression with multiple asyncs. 
        [Fact]
        public async Task MultipleAsync()
        {
            var func = new CustomAsyncTexlFunction("CustomAsync", DType.Number, DType.Number)
            {
                _impl = Worker
            };

            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var result = await engine.EvalAsync("If(CustomAsync(3)=6, CustomAsync(1)+CustomAsync(5), 99)", cts.Token);
            Assert.Equal(12.0, result.ToObject());
        }

        // Helper for creating a function that waits, and then returns 2x the result
        private class WaitHelper
        {
            private readonly TaskCompletionSource<FormulaValue> _waiter = new TaskCompletionSource<FormulaValue>();

            public async Task<FormulaValue> Worker2(FormulaValue[] args, CancellationToken cancel)
            {
                await Task.Yield();
                var result = await _waiter.Task;

                var n = ((NumberValue)result).Value;
                var x = FormulaValue.New(n * 2);

                return x;
            }

            public void SetResult(int value)
            {
                _waiter.SetResult(FormulaValue.New(value));
            }

            public TexlFunction GetFunction(string functionName)
            {
                return new CustomAsyncTexlFunction(functionName, DType.Number, DType.Number)
                {
                    _impl = Worker2
                };
            }
        }

        // Verify a custom function can return a non-completed task. 
        [Fact]
        public async Task VerifyFunctionIsAsync()
        {
            var helper = new WaitHelper();
            var func = helper.GetFunction("CustomAsync");

            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("CustomAsync(3)", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            Assert.False(task.IsCompleted);

            helper.SetResult(15);

            var result = await task;

            Assert.Equal(30.0, result.ToObject());
        }

        private static async Task<FormulaValue> WorkerWaitForCancel(FormulaValue[] args, CancellationToken cancel)
        {
            // Block forever until cancellation. 
            await Task.Delay(-1, cancel); // throws TaskCanceledException

            throw new InvalidOperationException($"Shouldn't get here");
        }

        // Verify cancellation 
        [Fact]
        public async Task Cancel()
        {
            var func = new CustomAsyncTexlFunction("CustomAsync", DType.Number, DType.Number)
            {
                _impl = WorkerWaitForCancel
            };

            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("CustomAsync(3)", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        // Test interleaved concurrent runs. 
        // RecalcEngine is single threaded, but the same engine should be able to do multiple evals.
        [Fact]
        public async Task ConcurrentInterleave()
        {
            var helper1 = new WaitHelper();
            var func1 = helper1.GetFunction("F1");

            var helper2 = new WaitHelper();
            var func2 = helper2.GetFunction("F2");

            var config1 = new PowerFxConfig(null);
            config1.AddFunction(func1);
            config1.AddFunction(func2);
            var engine = new RecalcEngine(config1);

            var task1 = engine.EvalAsync("F1(0)", CancellationToken.None);
            var task2 = engine.EvalAsync("F2(0)", CancellationToken.None);

            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);

            helper1.SetResult(333);
            helper2.SetResult(444);

            var result1 = await task1;
            var result2 = await task2;

            Assert.Equal(333.0 * 2, result1.ToObject());
            Assert.Equal(444.0 * 2, result2.ToObject());
        }

        // Verify WaitFor() function infrastructure
        [Fact]
        public async Task WaitForFunctionTest()
        {
            var helper = new WaitForFunctionsHelper(null);

            var result = await helper.Worker(0);
            Assert.Equal(0, result);

            var result1 = await helper.Worker(1);
            Assert.Equal(1, result1);

            // WaitFor(3) fails if WaitFor(2) wasn't called. 
            // Must do WaitFor() in order.
            await Assert.ThrowsAsync<InvalidOperationException>(() => helper.Worker(3));
        }

        // Veriy Async() function infrastructure
        [Fact]
        public async Task AsyncFunctionTest()
        {
            var helper = new AsyncFunctionsHelper(null);

            var result = await helper.Worker(0);
            Assert.Equal(0, result);

            // Async(2) won't complete until Async(1) is called. 
            var task2 = helper.Worker(2);
            Assert.False(task2.IsCompleted);

            await helper.Worker(1);

            var result2 = await task2;
            Assert.Equal(2, result2);
        }
    }

    // Helper for making async functions. 
    internal class CustomAsyncTexlFunction : TexlFunction, IAsyncTexlFunction
    {
        public Func<FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

        public CustomAsyncTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
            : this(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomAsyncTexlFunction(string name, DType returnType, params DType[] paramTypes)
            : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.MathAndStat, returnType, 0, paramTypes.Length, paramTypes.Length, paramTypes)
        {
        }

        public override bool IsSelfContained => true;

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { SG("Arg 1") };
        }

        public virtual Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            return _impl(args, cancel);
        }
    }
}
