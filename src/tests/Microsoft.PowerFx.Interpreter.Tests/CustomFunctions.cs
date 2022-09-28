// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Tests
{
    public class CustomFunctions : PowerFxTest
    {
        [Fact]
        public void CustomFunction()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCustomFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetAllFunctionNames().First(name => name == "TestCustom");
            Assert.NotNull(func);

            // Can be invoked. 
            var result = engine.Eval("TestCustom(3,true)");
            Assert.Equal("3,True", result.ToObject());
        }

        // Must have "Function" suffix. 
        private class TestCustomFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            public static StringValue Execute(NumberValue x, BooleanValue b)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        [Fact]
        public async void SimpleCustomAsyncFuntion()
        {
            var config = new PowerFxConfig(null);

            config.AddFunction(new TestCustomAsyncFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetAllFunctionNames().First(name => name == "TestCustomAsync");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var resultAsync = await engine.EvalAsync("TestCustomAsync(3, true)", cts.Token);

            Assert.Equal("3,True", resultAsync.ToObject());
        }

        // Verify a custom function can return a non-completed task. 
        [Fact]
        public async void VerifyCustomFunctionIsAsync()
        {
            var func = new TestCustomWaitAsyncFunction();
            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("TestCustomWaitAsync()", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            Assert.False(task.IsCompleted);

            func.SetResult(15);

            var result = await task;

            Assert.Equal(30.0, result.ToObject());
        }

        [Fact]
        public async void VerifyCancellationInAsync()
        {
            var func = new InfiniteAsyncFunction();
            var config = new PowerFxConfig(null);
            config.AddFunction(func);

            var engine = new RecalcEngine(config);
            using var cts = new CancellationTokenSource();

            await Task.Yield();
            var task = engine.EvalAsync("InfiniteAsync()", cts.Token);

            // custom func is blocking on our Infinite loop.
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        // Add a function that gets different runtime state per expression invoke
        [Fact]
        public async Task LocalAsyncFunction()
        {
            // Share a config
            var s1 = new SymbolTable();
            s1.AddFunction(new UserAsyncFunction());

            var engine = new RecalcEngine();

            var check = engine.Check(
                "UserAsync(3)",
                symbolTable: s1);

            // Bind expression once, and then can pass in per-eval state. 
            var expr = check.GetEvaluator();

            foreach (var name in new string[] { "Bill", "Steve", "Satya" })
            {
                var runtime = new SymbolValues()
                    .AddService(new UserAsyncFunction.Runtime { _name = name });
                var result = await expr.EvalAsync(CancellationToken.None, runtime);

                var expected = name + "3";
                var actual = result.ToObject();
                Assert.Equal(expected, actual);
            }
        }

        private class TestCustomAsyncFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            public static async Task<StringValue> Execute(NumberValue x, BooleanValue b, CancellationToken cancellationToken)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        private class TestCustomWaitAsyncFunction : ReflectionFunction
        {
            private readonly TaskCompletionSource<FormulaValue> _waiter = new TaskCompletionSource<FormulaValue>();

            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            public async Task<NumberValue> Execute(CancellationToken cancellationToken)
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
        }

        private class UserAsyncFunction : ReflectionFunction
        {
            public UserAsyncFunction()
            {
                // Specify the type used for config. 
                // At runtime, this is pulled from the RuntimeConfig config dictionary. 
                ConfigType = typeof(Runtime);
            }

            public class Runtime
            {
                public string _name;
            }

            // Must have "Execute" method. 
            // Arg0 is from RuntimeConfig state. 
            // Arg1 is a regular parameter. 
            // Cancellation Token must be the last argument for custom async function.
            public async Task<StringValue> Execute(Runtime config, NumberValue x, CancellationToken cancellationToken)
            {
                var val = x.Value;
                return FormulaValue.New(config._name + val);
            }
        }

        private class InfiniteAsyncFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            public async Task<StringValue> Execute(CancellationToken cancellationToken)
            {
                await Task.Delay(-1, cancellationToken); // throws TaskCanceledException

                throw new InvalidOperationException($"Shouldn't get here");
            }
        }

        public class TestObj
        {
            public double NumProp { get; set; }

            public bool BoolProp { get; set; }
        }

        private static readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public void CustomSetPropertyFunction()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCustomSetPropFunction());
            var engine = new RecalcEngine(config);

            var obj = new TestObj();
            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(obj);
            engine.UpdateVariable("x", x);

            // Test multiple overloads
            engine.Eval("SetProperty(x.NumProp, 123)", options: _opts);
            Assert.Equal(123.0, obj.NumProp);

            engine.Eval("SetProperty(x.BoolProp, true)", options: _opts);
            Assert.True(obj.BoolProp);

            // Test failure cases
            var check = engine.Check("SetProperty(x.BoolProp, true)"); // Binding Fail, behavior prop 
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.BoolProp, 123)"); // arg mismatch
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.numMissing, 123)", options: _opts); // Binding Fail
            Assert.False(check.IsSuccess);
        }

        // Must have "Function" suffix. 
        private class TestCustomSetPropFunction : ReflectionFunction
        {
            public TestCustomSetPropFunction()
                : base(SetPropertyName, FormulaType.Boolean)
            {
            }

            // Must have "Execute" method. 
            public static BooleanValue Execute(RecordValue source, StringValue propName, FormulaValue newValue)
            {
                var obj = (TestObj)source.ToObject();

                // Use reflection to set
                var prop = obj.GetType().GetProperty(propName.Value);
                prop.SetValue(obj, newValue.ToObject());

                return FormulaValue.New(true);
            }
        }

        // Verify we can add overloads of a custom function. 
        [Theory]
        [InlineData("SetField(123)", "SetFieldNumberFunction,123")]
        [InlineData("SetField(\"-123\")", "SetFieldStrFunction,-123")]
        [InlineData("SetField(\"abc\")", "SetFieldStrFunction,abc")]
        [InlineData("SetField(true)", "SetFieldNumberFunction,1")] // true coerces to number 1
        public void Overloads(string expr, string expected)
        {
            var config = new PowerFxConfig();
            config.AddFunction(new SetFieldNumberFunction());
            config.AddFunction(new SetFieldStrFunction());
            var engine = new RecalcEngine(config);

            var count = engine.GetAllFunctionNames().Count(name => name == "SetField");
            Assert.Equal(1, count); // no duplicates

            // Duplicates?
            var result = engine.Eval(expr);
            var actual = ((StringValue)result).Value;

            Assert.Equal(expected, actual);
        }

        private abstract class SetFieldBaseFunction : ReflectionFunction
        {
            public SetFieldBaseFunction(FormulaType fieldType) 
                : base("SetField", FormulaType.String, fieldType)
            {                
            }

            public StringValue Execute(FormulaValue newValue)
            {
                var overload = GetType().Name;
                var result = overload + "," + newValue.ToObject().ToString();
                return FormulaValue.New(result);
            }
        }

        private class SetFieldNumberFunction : SetFieldBaseFunction
        {
            public SetFieldNumberFunction()
                : base(FormulaType.Number)
            {
            }
        }

        private class SetFieldStrFunction : SetFieldBaseFunction
        {
            public SetFieldStrFunction()
                : base(FormulaType.String)
            {
            }
        }
    }
}
