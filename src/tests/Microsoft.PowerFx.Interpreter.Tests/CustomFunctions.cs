﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class CustomFunctions : PowerFxTest
    {
        public class CustomFunctionError : ReflectionFunction
        {
            public CustomFunctionError()
                : base("CustomFunctionError", FormulaType.Number, FormulaType.Number) 
            {
            }

            public static NumberValue Execute(NumberValue arg) 
            {
                if (arg.Value < 0)
                {
                    // CustomFunctionErrorException is caught and converted to error value.
                    throw new CustomFunctionErrorException("arg should be greater than 0");
                }
                else if (arg.Value == 0)
                {
                    // All other exceptions are uncaught and will abort the eval.
                    throw new NotSupportedException();
                }

                return arg;
            }
        }

        public class AsyncCustomFunctionError : ReflectionFunction
        {
            public AsyncCustomFunctionError()
                : base("CustomFunctionError2", FormulaType.Number, FormulaType.Number)
            {
            }

            public static async Task<NumberValue> Execute(NumberValue arg, CancellationToken cancellationToken)
            {
                if (arg.Value < 0)
                {
                    // CustomFunctionErrorException is caught and converted to error value.
                    throw new CustomFunctionErrorException("arg should be greater than 0");
                }
                else if (arg.Value == 0)
                {
                    // All other exceptions are uncaught and will abort the eval.
                    throw new NotSupportedException();
                }

                return arg;
            }
        }

        [Fact]
        public async Task CustomFunctionErrorTest()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new CustomFunctionError());
            config.AddFunction(new AsyncCustomFunctionError());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
#pragma warning disable CS0618 // Type or member is obsolete
            var func = engine.GetAllFunctionNames().First(name => name == "CustomFunctionError");
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.NotNull(func);

#pragma warning disable CS0618 // Type or member is obsolete
            var func2 = engine.GetAllFunctionNames().First(name => name == "CustomFunctionError2");
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.NotNull(func2);
            
            // Test for non async invokes.
            var result = engine.Eval("CustomFunctionError(20)");
            Assert.IsType<NumberValue>(result);
            Assert.Equal(20d, ((NumberValue)result).Value);

            var errorResult = engine.Eval("CustomFunctionError(-1)");
            Assert.IsType<ErrorValue>(errorResult);
            Assert.Equal("arg should be greater than 0", ((ErrorValue)errorResult).Errors.First().Message);

            Assert.Throws<AggregateException>(() => engine.Eval("CustomFunctionError(0)"));

            // Test for async invokes
            var result2 = engine.Eval("CustomFunctionError2(20)");
            Assert.IsType<NumberValue>(result2);
            Assert.Equal(20d, ((NumberValue)result2).Value);

            var errorResult2 = await engine.EvalAsync("CustomFunctionError2(-1)", CancellationToken.None).ConfigureAwait(false);
            Assert.IsType<ErrorValue>(errorResult2);
            Assert.Equal("arg should be greater than 0", ((ErrorValue)errorResult2).Errors.First().Message);

            await Assert.ThrowsAsync<NotSupportedException>(async () => await engine.EvalAsync("CustomFunctionError2(0)", CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        public class NullFunctionReturn : ReflectionFunction
        {
            public NullFunctionReturn()
                : base("NullFunction", FormulaType.Number)
            {
            }

            public static async Task<NumberValue> Execute(CancellationToken cancellationToken)
            {
                return null;
            }
        }

        [Fact]
        public async Task NullToBlankTest()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new NullFunctionReturn());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
#pragma warning disable CS0618 // Type or member is obsolete
            var func = engine.GetAllFunctionNames().First(name => name == "NullFunction");
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.NotNull(func);

            // Can be invoked. 
            var result = await engine.EvalAsync("NullFunction()", CancellationToken.None).ConfigureAwait(false);
            Assert.IsType<BlankValue>(result);
            Assert.IsType<NumberType>(result.Type);
        }

        [Fact]
        public void CustomFunction()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new TestCustomFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetFunctionsByName("TestCustom");
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
            var config = new PowerFxConfig();
            config.AddFunction(new TestCustomFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetFunctionsByName("TestCustom");
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
            // Must have "Execute" method. 
            public static StringValue Execute(NumberValue x, BooleanValue b, StringValue s)
            {
                var val = x.Value.ToString() + "," + b.Value.ToString() + "," + s.Value.ToString();
                return FormulaValue.New(val);
            }
        }

        [Fact]
        public void CustomRecordFunction()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new TestRecordCustomFunction());

            // Invalid function
            config.AddFunction(new TestInvalidRecordCustomFunction());

            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetFunctionsByName("RecordTest");
            Assert.NotNull(func);
            var invalidFunc = engine.GetFunctionsByName("InvalidRecordTest");
            Assert.NotNull(invalidFunc);

            // Can be invoked. 
            var result = engine.Eval("RecordTest()");
            Assert.IsNotType<ErrorValue>(result);

            var errorResult = engine.Eval("InvalidRecordTest()");
            Assert.IsType<ErrorValue>(errorResult);
        }

        // Must have "Function" suffix. 
        private class TestRecordCustomFunction : ReflectionFunction
        {
            public TestRecordCustomFunction()
                : base(
                      "RecordTest",
                      RecordType.Empty().Add(new NamedFormulaType("num", FormulaType.Number)))
            {
            }

            // Must have "Execute" method. 
            public static RecordValue Execute()
            {
                var record = RecordType.Empty()
                    .Add(new NamedFormulaType("num", FormulaType.Number));
                var val = FormulaValue.NewRecordFromFields(record, new NamedValue("num", FormulaValue.New(1)));
                return val;
            }
        }

        // Must have "Function" suffix. 
        private class TestInvalidRecordCustomFunction : ReflectionFunction
        {
            public TestInvalidRecordCustomFunction()
                : base(
                      "InvalidRecordTest",
                      RecordType.Empty().Add(new NamedFormulaType("num", FormulaType.Number)))
            {
            }

            // Must have "Execute" method. 
            public static RecordValue Execute()
            {
                var recordType = RecordType.Empty()
                    .Add(new NamedFormulaType("num1", FormulaType.Number));
                var val = FormulaValue.NewRecordFromFields(recordType, new NamedValue("num1", FormulaValue.New(1)));
                return val;
            }
        }

        [Theory]
        [InlineData("{x: 2}", true)]
        [InlineData("{x: 2, y: 5}", true)]
        [InlineData("{x: \"2\", y: \"5\"}", true)]
        [InlineData("{x: 2, y: 5, a: 8}", false)]
        [InlineData("{a: 8}", false)]
        [InlineData("{x: {x : 2}}", false)]
        public void CustomFunctionWithRecordTest(string inputRecord, bool isSuccess)
        {
            var emptyRecordType = RecordType.Empty();
            var recordType = RecordType.Empty().Add(new NamedFormulaType("x", FormulaType.Number)).Add(new NamedFormulaType("y", FormulaType.Number));
            var emptyTableType = emptyRecordType.ToTable();
            var tableType = recordType.ToTable();

            TestCustomFunctionWithRecordArgument(new TestAggregateIdentityCustomFunction<RecordType, RecordValue>(emptyRecordType), "(" + inputRecord + ")", true);
            TestCustomFunctionWithRecordArgument(new TestAggregateIdentityCustomFunction<RecordType, RecordValue>(recordType), "(" + inputRecord + ")", isSuccess);
            TestCustomFunctionWithRecordArgument(new TestAggregateIdentityCustomFunction<TableType, TableValue>(emptyTableType), "([" + inputRecord + "])", true);
            TestCustomFunctionWithRecordArgument(new TestAggregateIdentityCustomFunction<TableType, TableValue>(tableType), "([" + inputRecord + "])", isSuccess);
        }

        private void TestCustomFunctionWithRecordArgument(ReflectionFunction reflectionFunction, string inputRecord, bool isSuccess)
        {
            var config = new PowerFxConfig();
            config.AddFunction(reflectionFunction);
            
            var engine = new RecalcEngine(config);
            var func = engine.GetFunctionsByName(reflectionFunction.GetFunctionName());
            Assert.NotNull(func);

            var check = engine.Check(reflectionFunction.GetFunctionName() + inputRecord);

            FormulaValue result = null;
            FormulaValue resultEngineEval = null;
            ConfiguredTaskAwaitable<FormulaValue> resultAsync = new ConfiguredTaskAwaitable<FormulaValue>();
            string errorMsg = string.Empty;
            var rc = new RuntimeConfig();

            try
            {
                resultEngineEval = engine.Eval(reflectionFunction.GetFunctionName() + inputRecord); // Test the GetFormulaResult in ReflectionFunction.
                result = check.GetEvaluator().Eval(rc); // Test the CheckTypes in CustomTexlFunction.
                resultAsync = check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMsg = ex.ToString();
            }

            if (isSuccess)
            {
                Assert.True(check.IsSuccess);                
                Assert.IsNotType<ErrorValue>(resultEngineEval);
                Assert.IsNotType<ErrorValue>(result);
                Assert.IsNotType<ErrorValue>(resultAsync);
            }
            else
            {
                Assert.False(check.IsSuccess);
                Assert.Contains("invalid arguments", errorMsg);
            }
        }

        // Must have "Function" suffix. 
        private class TestAggregateIdentityCustomFunction<TType, TValue> : ReflectionFunction
            where TType : AggregateType
            where TValue : FormulaValue
        {
            public TestAggregateIdentityCustomFunction(TType argType)
                : base("RecordsTest", argType, argType)
            {
            }

            // Must have "Execute" method. 
            public static TValue Execute(TValue value)
            {
                return value;
            }
        }

        [Fact]
        public void CustomTableArgFunction()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new TableArgCustomFunction());

            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetAllFunctionNames().First(name => name == "TableArgTest");
            Assert.NotNull(func);

            // Can be invoked. 
            var result = engine.Eval("TableArgTest( [1, 2, 3, 4, 5] )");
            Assert.IsType<NumberValue>(result);

            var resultNumber = (NumberValue)result;
            Assert.Equal(5, resultNumber.Value);
        }

        private class TableArgCustomFunction : ReflectionFunction
        {
            public TableArgCustomFunction()
                : base("TableArgTest", FormulaType.Number, TableType.Empty().Add("Value", FormulaType.Number))
            {
            }

            public NumberValue Execute(TableValue table)
            {
                return FormulaValue.New(table.Count());
            }
        }

        [Fact]
        public void InvalidDeferredFunctionTest()
        {
            var config = new PowerFxConfig();
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
            var config = new PowerFxConfig();
            config.AddFunction(new TestCallbackFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetFunctionsByName("TestCallback");
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
            var config = new PowerFxConfig();
            config.AddFunction(new WaitFunction());
            config.AddFunction(new HelperFunction(x => FormulaValue.New(x.Value + 1)));
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetFunctionsByName("Wait");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var result = await engine.EvalAsync("Wait(Helper() = 3)", cts.Token).ConfigureAwait(false);
            Assert.Equal(true, result.ToObject());
        }

        private class WaitFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            // Any arg can be a boolean callback function.
            public static async Task<BooleanValue> Execute(Func<Task<BooleanValue>> expression, CancellationToken cancellationToken)
            {
                while (!(await expression().ConfigureAwait(false)).Value)
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
            var config = new PowerFxConfig();
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
                await expression().ConfigureAwait(false);
                return FormulaValue.New(false);
            }
        }

        [Fact]
        public async void CustomMockAndFunction_CallBack()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new MockAnd2ArgFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enuemeration
            var func = engine.GetFunctionsByName("MockAnd2Arg");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var result = engine.EvalAsync("MockAnd2Arg(1=2, 1=1)", cts.Token);
            Assert.Equal(false, (await result.ConfigureAwait(false)).ToObject());

            var result2 = engine.EvalAsync("MockAnd2Arg(1=1, 1=1)", cts.Token);
            Assert.Equal(true, (await result2.ConfigureAwait(false)).ToObject());
        }

        private class MockAnd2ArgFunction : ReflectionFunction
        {
            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            // Any arg can be a boolean callback function.
            public static async Task<BooleanValue> Execute(Func<Task<BooleanValue>> expression1, Func<Task<BooleanValue>> expression2, CancellationToken cancellationToken)
            {
                if (!(await expression1().ConfigureAwait(false)).Value)
                {
                    return FormulaValue.New(false);
                }

                return await expression2().ConfigureAwait(false);
            }
        }

        [Fact]
        public async void SimpleCustomAsyncFuntion()
        {
            var config = new PowerFxConfig();

            config.AddFunction(new TestCustomAsyncFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetFunctionsByName("TestCustomAsync");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var resultAsync = await engine.EvalAsync("TestCustomAsync(3, true)", cts.Token).ConfigureAwait(false);

            Assert.Equal("3,True", resultAsync.ToObject());
        }

        // Verify a custom function can return a non-completed task. 
        [Fact]
        public async void VerifyCustomFunctionIsAsync()
        {
            var func = new TestCustomWaitAsyncFunction();
            var config = new PowerFxConfig();
            config.AddFunction(func);

            var engine = new RecalcEngine(config);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            var task = engine.EvalAsync("TestCustomWaitAsync()", cts.Token);
            await Task.Yield();

            // custom func is blocking on our waiter
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
            Assert.False(task.IsCompleted);

            func.SetResult(15);

            var result = await task.ConfigureAwait(false);

            Assert.Equal(30.0, result.ToObject());
        }

        [Fact]
        public async void VerifyCancellationInAsync()
        {
            var func = new InfiniteAsyncFunction();
            var config = new PowerFxConfig();
            config.AddFunction(func);

            var engine = new RecalcEngine(config);
            using var cts = new CancellationTokenSource();

            await Task.Yield();
            var task = engine.EvalAsync("InfiniteAsync()", cts.Token);

            // custom func is blocking on our Infinite loop.
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await task.ConfigureAwait(false); }).ConfigureAwait(false);
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
                var runtime = new RuntimeConfig();
                runtime.AddService(new UserAsyncFunction.Runtime { _name = name });
                var result = await expr.EvalAsync(CancellationToken.None, runtime).ConfigureAwait(false);

                var expected = name + "3";
                var actual = result.ToObject();
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async void InvalidAsyncFunction()
        {
            var func = new TestCustomInvalidAsyncFunction();
            var config = new PowerFxConfig();
            Assert.Throws<InvalidOperationException>(() => config.AddFunction(func));
        }

        [Fact]
        public async void InvalidAsync2Function()
        {
            var func = new TestCustomInvalid2AsyncFunction();
            var config = new PowerFxConfig();
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
            var config = new PowerFxConfig();

            config.AddFunction(new TestCtorCustomAsyncFunction());
            var engine = new RecalcEngine(config);

            // Shows up in enumeration
            var func = engine.GetFunctionsByName("CustomAsync");
            Assert.NotNull(func);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();
            var resultAsync = await engine.EvalAsync("CustomAsync(\"test\")", cts.Token).ConfigureAwait(false);

            Assert.Equal("test", resultAsync.ToObject());
        }

        private class TestCtorCustomAsyncFunction : ReflectionFunction
        {
            public TestCtorCustomAsyncFunction() 
                : base("CustomAsync", FormulaType.String, FormulaType.String)
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
                var result = await _waiter.Task.ConfigureAwait(false);

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
                await Task.Delay(-1, cancellationToken).ConfigureAwait(false); // throws TaskCanceledException

                throw new InvalidOperationException($"Shouldn't get here");
            }
        }

        public class TestObj
        {
            public double NumProp { get; set; }

            public bool BoolProp { get; set; }

            public decimal DecimalProp { get; set; }
        }

        private static readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public void CustomSetPropertyFunction()
        {
            var floatOptions = new ParserOptions { NumberIsFloat = true, AllowsSideEffects = true };
            var decimalOptions = new ParserOptions { NumberIsFloat = false, AllowsSideEffects = true };

            var config = new PowerFxConfig();
            config.AddFunction(new TestCustomSetPropFunction());
            var engine = new RecalcEngine(config);

            var obj = new TestObj();
            var cache = new TypeMarshallerCache();
            var x = cache.Marshal(obj);
            engine.UpdateVariable("x", x);

            var expressions = new[] { "Float(123)", "Decimal(124)", "125" };
            CheckFloatCoercions(engine, obj, floatOptions, expressions);
            CheckDecimalCoercions(engine, obj, decimalOptions, expressions);

            // Tests multiple overloads
            engine.Eval("SetProperty(x.BoolProp, true)", options: _opts);
            Assert.True(obj.BoolProp);

            // Test failure cases
            var check = engine.Check("SetProperty(x.BoolProp, true)"); // Binding Fail, behavior prop 
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.BoolProp, Float(123))", options: _opts); // arg mismatch
            Assert.False(check.IsSuccess);

            check = engine.Check("SetProperty(x.numMissing, Float(123))", options: _opts); // Binding Fail
            Assert.False(check.IsSuccess);
        }

        private void CheckFloatCoercions(RecalcEngine engine, TestObj obj, ParserOptions options, string[] expressions)
        {
            var expectedValues = new[] { 123.0, 124.0, 125.0 };
            for (int i = 0; i < expressions.Length; i++)
            {
                engine.Eval($"SetProperty(x.NumProp, {expressions[i]})", null, options);
                Assert.Equal(expectedValues[i], obj.NumProp);
            }
        }

        private void CheckDecimalCoercions(RecalcEngine engine, TestObj obj, ParserOptions options, string[] expressions)
        {
            var expectedValues = new[] { 123.0m, 124.0m, 125.0m };
            for (int i = 0; i < expressions.Length; i++)
            {
                engine.Eval($"SetProperty(x.DecimalProp, {expressions[i]})", null, options);
                Assert.Equal(expectedValues[i], obj.DecimalProp);
            }
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

            var setFieldFunctions = engine.GetFunctionsByName("SetField");
            Assert.Equal(2, setFieldFunctions.Count()); 

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
