// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

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
            var result = engine.Eval("TestCustom(3,true,\"a\")");
            Assert.Equal("3,True,a", result.ToObject());
        }

        [Theory]
        [InlineData("TestCustom(1/0,true,\"test\")", null, true, "Invalid operation: division by zero.")]

        // With Blanks() as arg where expected arg is not a number or a string, Blank() will generate type mismatch error.
        [InlineData("TestCustom(0, If(false,true), \"test\")", null, true, "Runtime type mismatch")]

        // With Blanks() as arg where expected arg is number, Blank() will be coerced to 0.
        [InlineData("TestCustom(If(false,12),true,\"test\")", "0,True,test", false, null)]

        // With Blanks() as arg where expected arg is string, Blank() will be coerced to empty string.
        [InlineData("TestCustom(0,true,If(false,\"test\"))", "0,True,", false, null)]
        public void CustomFunctionErrorOrBlank(string script, string expectedResult, bool isErrorExpected, string errorMessage)
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCustomFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetAllFunctionNames().First(name => name == "TestCustom");
            Assert.NotNull(func);

            // With error as arg.
            var result = engine.Eval(script);

            if (isErrorExpected)
            {
                Assert.IsType<ErrorValue>(result);
                Assert.Equal(1, ((ErrorValue)result).Errors.Count);
                Assert.Equal(errorMessage, ((ErrorValue)result).Errors[0].Message);
            }
            else
            {
                Assert.Equal(expectedResult, result.ToObject());
            }
        }

        // Must have "Function" suffix. 
        private class TestCustomFunction : ReflectionFunction
        {
            public TestCustomFunction():
                base("TestCustom", FormulaType.Boolean, FormulaType.Number, FormulaType.Boolean, FormulaType.String)
            {

            }
            // Must have "Execute" method. 
            public static StringValue Execute(NumberValue x, BooleanValue b, StringValue s)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString() + "," + s.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        [Fact]
        public void InvalidDeferredFunctionTest()
        {
            var config = new PowerFxConfig(null);
            Assert.Throws<NotSupportedException>(() => config.AddFunction(new InvalidDeferredFunction()));
            Assert.Throws<NotSupportedException>(() => config.AddFunction(new InvalidArgDeferredFunction()));
        }

        private class InvalidDeferredFunction : ReflectionFunction
        {
            public InvalidDeferredFunction()
                : base("InvalidDeferred", FormulaType.Deferred)
            {
            }

            public StringValue Execute()
            {
                return FormulaValue.New("test");
            }
        }

        private class InvalidArgDeferredFunction : ReflectionFunction
        {
            public InvalidArgDeferredFunction()
                : base("InvalidDeferred", FormulaType.String, FormulaType.Deferred)
            {
            }

            public StringValue Execute(StringValue stringValue)
            {
                return FormulaValue.New("test");
            }
        }

        [Fact]
        public async void CustomFunction_CallBack()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new TestCallbackFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetAllFunctionNames().First(name => name == "TestCallback");
            Assert.NotNull(func);

            var result = engine.Eval("TestCallback(1=2)");
            Assert.Equal(false, result.ToObject());
        }

        private class TestCallbackFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Any arg can be a boolean callback function.
            public static BooleanValue Execute(Func<Task<BooleanValue>> expression)
            {
                return expression().Result;
            }
        }

        [Fact]
        public async void CustomFunctionAsync_CallBack()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new WaitFunction());
            config.AddFunction(new HelperFunction(x => FormulaValue.New(x.Value + 1)));
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetAllFunctionNames().First(name => name == "Wait");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var result = await engine.EvalAsync("Wait(Helper() = 3)", cts.Token);
            Assert.Equal(true, result.ToObject());
        }

        private class WaitFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            // Any arg can be a boolean callback function.
            public static async Task<BooleanValue> Execute(Func<Task<BooleanValue>> expression, CancellationToken cancellationToken)
            {
                while (!(await expression()).Value)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return FormulaValue.New(true);
            }
        }

        private class HelperFunction : ReflectionFunction
        {
            private readonly Func<NumberValue, NumberValue> _func;
            private NumberValue _counter;

            public HelperFunction(Func<NumberValue, NumberValue> func)
            {
                _func = func;
                _counter = FormulaValue.New(0);
            }

            public NumberValue Execute()
            {
                _counter = _func(_counter);
                return _counter;
            }
        }

        [Fact]
        public async void CustomFunctionAsync_CallBack_Invalid()
        {
            var config = new PowerFxConfig(null);
            Action act = () => config.AddFunction(new InvalidTestCallbackFunction());
            Exception exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Equal("Unknown parameter type: expression, System.Func`1[System.Threading.Tasks.Task`1[Microsoft.PowerFx.Types.StringValue]]. Only System.Func`1[System.Threading.Tasks.Task`1[Microsoft.PowerFx.Types.BooleanValue]] is supported", exception.Message);
        }

        private class InvalidTestCallbackFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            // Any arg can be a boolean callback function.
            public static async Task<BooleanValue> Execute(Func<Task<StringValue>> expression, CancellationToken cancellationToken)
            {
                await expression();
                return FormulaValue.New(false);
            }
        }

        [Fact]
        public async void CustomMockAndFunction_CallBack()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(new MockAnd2ArgFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetAllFunctionNames().First(name => name == "MockAnd2Arg");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var result = engine.EvalAsync("MockAnd2Arg(1=2, 1=1)", cts.Token);
            Assert.Equal(false, (await result).ToObject());

            var result2 = engine.EvalAsync("MockAnd2Arg(1=1, 1=1)", cts.Token);
            Assert.Equal(true, (await result2).ToObject());
        }

        private class MockAnd2ArgFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            // Any arg can be a boolean callback function.
            public static async Task<BooleanValue> Execute(Func<Task<BooleanValue>> expression1, Func<Task<BooleanValue>> expression2, CancellationToken cancellationToken)
            {
                if (!(await expression1()).Value)
                {
                    return FormulaValue.New(false);
                }

                return await expression2();
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
        public async void LocalAsyncFunction()
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

        [Fact]
        public async void InvalidAsyncFunction()
        {
            var func = new TestCustomInvalidAsyncFunction();
            var config = new PowerFxConfig(null);
            Assert.Throws<InvalidOperationException>(() => config.AddFunction(func));
        }

        [Fact]
        public async void InvalidAsync2Function()
        {
            var func = new TestCustomInvalid2AsyncFunction();
            var config = new PowerFxConfig(null);
            Assert.Throws<InvalidOperationException>(() => config.AddFunction(func));
        }

        private class TestCustomInvalidAsyncFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function, which is not
            // hence the invalid
            public static async Task<StringValue> Execute(NumberValue x, BooleanValue b)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        private class TestCustomInvalid2AsyncFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function, which is not
            // hence the invalid
            public static async Task<StringValue> Execute(NumberValue x, CancellationToken cancellationToken, BooleanValue b)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString();
                return FormulaValue.New(val);
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

        [Fact]
        public async void CustomAsyncFuntionUsingCtor()
        {
            var config = new PowerFxConfig(null);

            config.AddFunction(new TestCtorCustomAsyncFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetAllFunctionNames().First(name => name == "CustomAsync");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var resultAsync = await engine.EvalAsync("CustomAsync(\"test\")", cts.Token);

            Assert.Equal("test", resultAsync.ToObject());
        }

        private class TestCtorCustomAsyncFunction : ReflectionFunction
        {

            public TestCtorCustomAsyncFunction() :
                base("CustomAsync", FormulaType.Boolean, FormulaType.String)
            {

            }

            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            public async Task<StringValue> Execute(StringValue input, CancellationToken cancellationToken)
            {
                return FormulaValue.New("test");
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
