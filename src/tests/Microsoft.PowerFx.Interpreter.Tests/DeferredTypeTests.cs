// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// Assumptions on Deferred Type:
    /// 1. you can't construct deferred type around aggregate type e.g. Record type, TableType
    /// 2. Function calls discard all the errors if any arg present is deferred type. e.g. Sum(deferred, Record) would not give any error for Record Type.
    /// 3. Function Overload resolution is biased to scaler type, because we discard error and make the first overload valid it matches to scaler one and 
    /// does not try and go to Tabular overload. e.g. Sum(deferred) return type is number, and not the table of numbers.
    /// </summary>
    public class DeferredTypeTests
    {
        [Theory]
        [InlineData("X", "X")]
        [InlineData("$\"Test {X}\"", "s")]
        [InlineData("!X", "b")]
        [InlineData("Table(X)", "X")]
        [InlineData("X.Field1", "X")]

        [InlineData("X + 1", "X")]
        [InlineData("X + \"1\"", "X")]
        [InlineData("X + DateTime(2022, 11, 10, 0, 0, 0)", "d")]
        [InlineData("X + Date(2022, 11, 10)", "d")]
        [InlineData("X + Time(0, 0, 0)", "T")]
        [InlineData("1 + X", "X")]
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

        [InlineData("Sum(X, X)", "n")]
        [InlineData("X.Field1.Field1", "X")]
        [InlineData("If(true, X)", "X")]
        [InlineData("If(true, X, 1)", "n")]
        [InlineData("If(true, X, X)", "X")]
        [InlineData("ForAll([1,2,3], X)", "X")]
        [InlineData("ForAll(ParseJSON(\"[1]\"), X)", "X")]
        [InlineData("Abs(Table(X))", "n")]
        [InlineData("Power(2, Table(X))", "n")]
        [InlineData("Switch(X, 0, 0, 1, 1)", "n")]
        [InlineData("Switch(0, 0, X, 1, 1)", "n")]
        [InlineData("Switch(0, 0, X, 1, X)", "X")]
        [InlineData("Switch(0, 0, X, 1, \"test\")", "s")]
        [InlineData("Set(N, X); N", "n")]
        [InlineData("Set(N, X); Set(N, 5); N", "n")]
        [InlineData("Set(XM, X); XM", "X")]

        // Ensures expression binds without any errors - but issues a warning for the deferred(unknown) type.
        public void DeferredTypeTest(string script, string expectedReturnType)
        {
            var symbolTable = new SymbolTable();

            symbolTable.AddVariable("X", FormulaType.Deferred);
            symbolTable.AddVariable("R", RecordType.Empty());
            symbolTable.AddVariable("T", RecordType.Empty().ToTable());
            symbolTable.AddVariable("N", FormulaType.Number, mutable: true);
            symbolTable.AddVariable("XM", FormulaType.Deferred, mutable: true);

            TestDeferredTypeBindingWarning(script, Features.None, TestUtils.DT(expectedReturnType), symbolTable);
        }

        [Theory]
        [InlineData("$\"Test {X} {R}\"", "ErrBadType_ExpectedType_ProvidedType")]
        [InlineData("X + R", "ErrBadType_ExpectedTypesCSV")]
        [InlineData("X.field + N.field", "ErrInvalidDot")]
        [InlineData("Index([1,2,3], X).missing", "ErrInvalidName")]
        [InlineData("X < \"2021-12-09T20:28:52Z\"", "ErrBadType_ExpectedTypesCSV")]
        [InlineData("First(Sum(X, 1))", "ErrBadType_ExpectedType_ProvidedType")]

        // Can't create aggregates around Deferred type.
        [InlineData("[X]", "ErrTableDoesNotAcceptThisType")]
        [InlineData("{test: X}", "ErrRecordDoesNotAcceptThisType")]
        [InlineData("{ a: { a: { a: X } } }", "ErrRecordDoesNotAcceptThisType")]
        [InlineData("Table({ a: X })", "ErrRecordDoesNotAcceptThisType")]
        [InlineData("Table({ a: { a: { a: X } } })", "ErrRecordDoesNotAcceptThisType")]

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

            config.EnableSetFunction();
            config.EnableParseJSONFunction();

            var engine = new RecalcEngine(config);
            var result = engine.Check(script, options: new ParserOptions() { AllowsSideEffects = true });
            
            Assert.True(result.IsSuccess);

            var returnType = FormulaType.Build(result._binding.ResultType);

            Assert.Equal(expected, result._binding.ResultType);

            Assert.True(result.Errors.Count() > 0);

            Assert.True(result.Errors.All(error => error.MessageKey.Equals(TexlStrings.WarnDeferredType.Key)));

            Assert.Throws<NotSupportedException>(() => CheckResultExtensions.GetEvaluator(result));
            Assert.Throws<AggregateException>(() => engine.Eval(script));
        }

        private void TestBindingError(string script, Features features, string errorMessageKey, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.False(result.IsSuccess);

            Assert.Contains(result.Errors, error => error.MessageKey.Equals(errorMessageKey));
        }
    }
}
