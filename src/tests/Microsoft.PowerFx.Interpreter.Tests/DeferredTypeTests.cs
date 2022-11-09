// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter
{
    public class DeferredTypeTests
    {
        [Theory]
        [InlineData("X")]
        [InlineData("$\"Test {X}\"")]
        [InlineData("!X")]
        [InlineData("Table(X)")]
        [InlineData("X.Field1")]
        [InlineData("X + 1")]
        [InlineData("X + Date(2022, 10, 27)")]
        [InlineData("X * 1")]
        [InlineData("X < 2")]
        [InlineData("\"The\" in X")]
        [InlineData("123 in X")]
        [InlineData("123 in Table(X)")]
        [InlineData("IsBlank(X)")]
        [InlineData("IsError(X)")]
        [InlineData("Text(X)")]
        [InlineData("Value(X)")]
        [InlineData("Boolean(X)")]
        [InlineData("Index([1,2,3].Value, X)")]

        // Ensures expression binds without any errors - but issues a warning for the deferred(unknown) type.
        public void DeferredTypeTest_EnableDeferredType(string script)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            TestDeferredTypeBindingWarning(script, symbolTable);
        }

        [Theory]
        [InlineData("$\"Test {X} {R}\"", "Invalid argument type (Record). Expecting a Text value instead.")]
        [InlineData("Table(X, N)", "Cannot use a non-record value in this context")]
        [InlineData("X.field + N.field", "Invalid use of '.'")]
        [InlineData("Index([1,2,3], X).missing", "Name isn't valid. 'missing' isn't recognized")]

        // Ensures expression issues an error if it exists, despite the deferred type.
        public void DeferredTypeTest_EnableDeferredType_Negative(string script, string errorMessage)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            symbolTable.AddVariable("N", FormulaType.Number);
            symbolTable.AddVariable("R", RecordType.Empty());
            TestBindingError(script, errorMessage, symbolTable);
        }

        private void TestDeferredTypeBindingWarning(string script, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig()
            {
                SymbolTable = symbolTable
            };

            var engine = new RecalcEngine(config);
            var result = engine.Check(script, new ParserOptions() { AllowDeferredType = true });

            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count() > 0);
            Assert.True(result.Errors.All(error => error.MessageKey.Equals(TexlStrings.WarnUnknownType.Key)));

            Assert.Throws<NotSupportedException>(() => CheckResultExtensions.GetEvaluator(result));
            Assert.Throws<AggregateException>(() => engine.Eval(script));
        }

        private void TestBindingError(string script, string errorMessage, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig()
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script, new ParserOptions() { AllowDeferredType = true });

            Assert.False(result.IsSuccess);

            Assert.Contains(result.Errors, error => error.Message.Contains(errorMessage));
        }
    }
}
