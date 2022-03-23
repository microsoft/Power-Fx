// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;
using Xunit.Sdk;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Tests
{
    // Test async eval features. 
    public class RecalcEngineAsyncTests
    {
        // Trivial async function that runs synchronously. 
        private static async Task<FormulaValue> Worker(FormulaValue[] args, CancellationToken cancel)
        {
            var n = (NumberValue)args[0];

            var result = FormulaValue.New(n.Value * 2);
            return result;
        }

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
            var cts = new CancellationTokenSource();

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
            var cts = new CancellationTokenSource();

            var result = await engine.EvalAsync("If(CustomAsync(3)=6, CustomAsync(1)+CustomAsync(5), 99)", cts.Token);
            Assert.Equal(12.0, result.ToObject());
        }

        private class Helper
        {
            public TaskCompletionSource<FormulaValue> _waiter = new TaskCompletionSource<FormulaValue>();

            public async Task<FormulaValue> Worker2(FormulaValue[] args, CancellationToken cancel)
            {
                await Task.Yield();
                var result = await _waiter.Task;

                var n = ((NumberValue)result).Value;
                var x = FormulaValue.New(n * 2);

                return x;
            }
        }

        // Verify a custom function can return a non-completed task. 
        [Fact]
        public async Task VerifyFunctionIsAsync()
        {
            var helper = new Helper();
            var func = new CustomAsyncTexlFunction("CustomAsync", DType.Number, DType.Number)
            {
                _impl = helper.Worker2
            };

            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("CustomAsync(3)", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5)); 
            Assert.False(task.IsCompleted);

            helper._waiter.SetResult(FormulaValue.New(15));

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
            var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("CustomAsync(3)", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        // Verify cancellation 
        [Fact]
        public async Task InfiniteLoop()
        {
            // Create an expression that will take hang (take very long)
            var n = 10 * 1000;
            var expr = $"ForAll(Sequence({n}), ForAll(Sequence({n}), ForAll(Sequence({n}), 5)))";

            var engine = new RecalcEngine();

            // Can be invoked. 
            var cts = new CancellationTokenSource();

            cts.CancelAfter(5);

            // Eval may never return.             
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await engine.EvalAsync(expr, cts.Token);
            });
        }

        // Helper for making async functions. 
        internal class CustomAsyncTexlFunction : TexlFunction, IAsyncTexlFunction
        {
            public Func<FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

            public override bool SupportsParamCoercion => true;

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
}
