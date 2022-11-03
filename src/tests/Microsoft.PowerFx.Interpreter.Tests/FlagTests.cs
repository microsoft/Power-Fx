// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Preview;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FlagTests
    {
        /// <summary>
        /// "@ syntax is deprecated. Now, users should write 
        /// Filter(A, ThisRecord.Value = 2) or Filter(A As Foo, Foo.Value = 2)
        /// instead of
        /// Filter(A, A[@Value] = 2).
        /// </summary>
        /// <param name="expression"></param>
        [Theory]
        [InlineData("Sum(A, A[@Value])")]
        [InlineData("Filter(A, A[@Value] = 2)")]
        public void Parser_DisableRowScopeDisambiguationSyntax(string expression)
        {
            var engine = new RecalcEngine(new PowerFxConfig(features: Features.DisableRowScopeDisambiguationSyntax));
            var engineWithoutFlag = new RecalcEngine(new PowerFxConfig());
            
            NumberValue r1 = FormulaValue.New(1);
            NumberValue r2 = FormulaValue.New(2);
            NumberValue r3 = FormulaValue.New(3);
            TableValue val = FormulaValue.NewSingleColumnTable(r1, r2, r3);

            engine.UpdateVariable("A", val);
            var resultWithFlag = engine.Parse(expression);

            engineWithoutFlag.UpdateVariable("A", val);
            var resultWithoutFlag = engineWithoutFlag.Parse(expression);

            Assert.False(resultWithFlag.IsSuccess);
            Assert.NotNull(resultWithFlag.Errors.First(error => error.MessageKey.Equals(TexlStrings.ErrDeprecated.Key)));

            Assert.True(resultWithoutFlag.IsSuccess);
        }

        [Theory]
        [InlineData("X")]
        [InlineData("$\"Test {X}\"")]
        [InlineData("!X")]
        [InlineData("First(Table(X))")]
        [InlineData("X.Field1")]
        [InlineData("X + 1")]
        [InlineData("X + Date(2022, 10, 27)")]
        [InlineData("X * 1")]
        [InlineData("X < 2")]
        [InlineData("\"The\" in X")]
        [InlineData("123 in X")]
        [InlineData("123 in Table(X)", false)]
        [InlineData("IsBlank(X)")]
        [InlineData("IsError(X)", false)]
        [InlineData("Text(X)")]
        [InlineData("Value(X)")]
        [InlineData("Boolean(X)")]
        [InlineData("Index([1,2,3].Value, X)")]

        // Ensures expression binds without any errors - but issues a warning for the deferred(unknown) type.
        public void DeferredTypeTest_EnableDeferredType(string script, bool isEvalAllowed = true)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            TestDeferredTypeBindingWarning(script, Features.EnableDeferredType, symbolTable, isEvalAllowed);
        }

        [Theory]
        [InlineData("$\"Test {X} {R}\"")]
        [InlineData("Table(X, N)")]
        [InlineData("X.field + N.field")]
        [InlineData("Index([1,2,3], X).missing")]

        // Ensures expression issues an error if it exists, despite the deferred type.
        public void DeferredTypeTest_EnableDeferredType_Negative(string script)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            symbolTable.AddVariable("N", FormulaType.Number);
            symbolTable.AddVariable("R", RecordType.Empty());
            TestBindingError(script, Features.EnableDeferredType, symbolTable);
        }

        private void TestDeferredTypeBindingWarning(string script, Features features, SymbolTable symbolTable = null, bool isEvalAllowed = true)
        {
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new RecalcEngine(config);
            var result = engine.Check(script);

            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count() > 0);
            Assert.True(result.Errors.All(error => error.MessageKey.Equals(TexlStrings.WarnUnknownType.Key)));

            if (isEvalAllowed)
            {
                var evaluator = CheckResultExtensions.GetEvaluator(result);
                var evaluatorResult = evaluator.Eval();
                Assert.IsType<ErrorValue>(evaluatorResult);
            }
        }

        private void TestBindingError(string script, Features features, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.False(result.IsSuccess);
        }
    }
}
