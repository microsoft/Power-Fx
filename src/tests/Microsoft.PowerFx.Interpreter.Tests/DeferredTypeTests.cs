// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter
{
    public class DeferredTypeTests
    {
        [Theory]
        [InlineData("X", "X")]
        [InlineData("$\"Test {X}\"", "s")]
        [InlineData("!X", "b")]
        [InlineData("Table(X)", "X")]
        [InlineData("X.Field1", "X")]

        [InlineData("X + 1", "X")] // resolve
        [InlineData("X + \"1\"", "X")] // resolve
        [InlineData("X + DateTime(2022, 11, 10, 0, 0, 0)", "d")]
        [InlineData("X + Date(2022, 11, 10)", "d")]
        [InlineData("X + Time(0, 0, 0)", "T")]
        [InlineData("1 + X", "X")] // resolve
        [InlineData("DateTime(2022, 11, 10, 0, 0, 0) + X", "d")]
        [InlineData("Date(2022, 11, 10) + X", "d")]
        [InlineData("Time(0, 0, 0) + X", "T")]

        [InlineData("X * 1", "n")]
        [InlineData("And(X, 1=1)", "b")]
        [InlineData("X&\"test\"", "s")]
        [InlineData("X = 1", "b")]
        [InlineData("X = GUID(\"5cc45615-f759-4a53-b225-d3a2497f60ad\")", "b")]

        [InlineData("X < 2", "b")]
        [InlineData("X < DateTime(2022,11,10,0,0,0)", "b")]
        [InlineData("\"The\" in X", "b")]
        [InlineData("123 in X", "b")]
        [InlineData("X in [1, 2, 3]", "b")]
        [InlineData("123 in Table(X)", "b")]
        [InlineData("IsBlank(X)", "b")]
        [InlineData("IsError(X)", "b")]
        [InlineData("Text(X)", "s")]
        [InlineData("Value(X)", "n")]
        [InlineData("Boolean(X)", "b")]
        [InlineData("Index([1,2,3].Value, X)", "![Value:n]")]
        [InlineData("Sum(X, 1) < 5", "b")]
        [InlineData("Sum(X, 1, R) < 5", "b")] // All error are discarded for function calls, hence we don't get error for RecordType here.
        [InlineData("Sum(X, T) < 5", "b")] // Since we discard all errors for function calls, Function calls are biased to non tabular overload.

        // Ensures expression binds without any errors - but issues a warning for the deferred(unknown) type.
        public void DeferredTypeTest(string script, string expectedReturnType)
        {
            var symbolTable = new SymbolTable();

            symbolTable.AddVariable("X", FormulaType.Deferred);
            symbolTable.AddVariable("R", RecordType.Empty());
            symbolTable.AddVariable("T", RecordType.Empty().ToTable());

            TestDeferredTypeBindingWarning(script, Features.None, TestUtils.DT(expectedReturnType), symbolTable);
        }

        [Theory]
        [InlineData("$\"Test {X} {R}\"", "Invalid argument type (Record). Expecting a Text value instead.")]
        [InlineData("X + R", "Invalid argument type. Expecting one of the following: Number, Text, Boolean.")]
        [InlineData("X.field + N.field", "Invalid use of '.'")]
        [InlineData("Index([1,2,3], X).missing", "Name isn't valid. 'missing' isn't recognized")]
        [InlineData("X < \"2021-12-09T20:28:52Z\"", "Invalid argument type. Expecting one of the following: Number, Date, Time, DateTime.")]
        [InlineData("First(Sum(X, 1))", "Invalid argument type (Number). Expecting a Table value instead.")]

        // Can't create aggregates around Deferred type.
        [InlineData("[X]", "Incompatible type. The item you are trying to put into a table has a type that is not compatible with the table.")]
        [InlineData("{test: X}", "Incompatible type. The item you are trying to put into a record has a type that is not compatible with the record.")]

        // Ensures expression issues an error if it exists, despite the deferred type.
        // NOTE: All error are discarded for function calls e.g. You don't get any errors for Table(deferred, number).
        public void DeferredTypeTest_Negative(string script, string errorMessage)
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Deferred);
            symbolTable.AddVariable("N", FormulaType.Number);
            symbolTable.AddVariable("R", RecordType.Empty());
            TestBindingError(script, Features.None, errorMessage, symbolTable);
        }

        [Fact]

        // Construction of aggregate type is not allowed around Deferred Type
        public void DeferredTypeNotSupportedInAggregate()
        {
            // Record Type
            Assert.Throws<NotSupportedException>(() => RecordType.Empty().Add("someName", FormulaType.Deferred));
            Assert.Throws<NotSupportedException>(() => RecordType.Empty().Add(new NamedFormulaType("someName", FormulaType.Deferred)));

            // Table Type
            Assert.Throws<NotSupportedException>(() => TableType.Empty().Add("someName", FormulaType.Deferred));
            Assert.Throws<NotSupportedException>(() => TableType.Empty().Add(new NamedFormulaType("someName", FormulaType.Deferred)));
        }

        private void TestDeferredTypeBindingWarning(string script, Features features, DType expected, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new RecalcEngine(config);
            var result = engine.Check(script);

            Assert.True(result.IsSuccess);

            var returnType = FormulaType.Build(result._binding.ResultType);

            Assert.Equal(expected, result._binding.ResultType);

            Assert.True(result.Errors.Count() > 0);

            Assert.True(result.Errors.All(error => error.MessageKey.Equals(TexlStrings.WarnDeferredType.Key)));

            Assert.Throws<NotSupportedException>(() => CheckResultExtensions.GetEvaluator(result));
            Assert.Throws<AggregateException>(() => engine.Eval(script));
        }

        private void TestBindingError(string script, Features features, string errorMessage, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.False(result.IsSuccess);

            Assert.Contains(result.Errors, error => error.Message.Contains(errorMessage));
        }
    }
}
