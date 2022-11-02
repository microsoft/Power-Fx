// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class TypeTests
    {
        [Fact]
        public void RecordTypeValidation()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var r2 = RecordType.Empty()
                .Add(new NamedFormulaType("B", FormulaType.Boolean))
                .Add(new NamedFormulaType("Num", FormulaType.Number));

            // Structural equivalence
            Assert.Equal(r1, r2);

            // Test op==
            Assert.True(r1 == r2);
            Assert.False(r2 == null);
            Assert.False(r1 == null);

            Assert.True(r1 != null);
            Assert.True(r1 != null);
            Assert.False(r1 != r2);

            Assert.True(r1.Equals(r2));
            Assert.False(r1.Equals(null));

            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        }

        [Theory]
        [InlineData("Display1", true, "F1")]
        [InlineData("F1", true, "F1")]
        [InlineData("SomethingElse", false, null)]
        public void TryGetFieldTypeLookupTest(string inputDisplayOrLogical, bool succeeds, string expectedLogical)
        {
            var r1 = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Number, "Display1"));

            Assert.Equal(r1.TryGetFieldType(inputDisplayOrLogical, out var actualLogical, out var formulaType), succeeds);
            
            Assert.Equal(expectedLogical, actualLogical);
     
            // Since, it returns Blank node on returning false too.
            Assert.NotNull(formulaType);
        }

        [Theory]
        [InlineData("F1", true, "F1")]
        [InlineData("SomethingElse", false, null)]
        public void TryGetFieldTypeLookupTestWithoutDisplayName(string inputDisplayOrLogical, bool succeeds, string expectedLogical)
        {
            var r1 = RecordType.Empty().Add(new NamedFormulaType("F1", FormulaType.Number));

            Assert.Equal(r1.TryGetFieldType(inputDisplayOrLogical, out var actualLogical, out var formulaType), succeeds);

            Assert.Equal(expectedLogical, actualLogical);

            // Since, it returns Blank node on returning false too.
            Assert.NotNull(formulaType);
        }

        [Fact]
        public void TryGetFieldTypeLookupNullTest()
        {
            var r1 = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Number, "Display1"));

            Assert.Throws<ArgumentNullException>(
                () => r1.TryGetFieldType(null, out var actualLogical, out var formulaType));

            Assert.Throws<ArgumentException>(
                () => r1.TryGetFieldType(string.Empty, out var actualLogical, out var formulaType));
        }

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
        public void DeferredTypeTest(string script)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            TestDeferredTypeBindingWarning(script, symbolTable);
        }

        [Theory]
        [InlineData("$\"Test {X} {R}\"")]
        [InlineData("Table(X, N)")]
        [InlineData("X.field + N.field")]
        [InlineData("Index([1,2,3], X).missing")]

        // Ensures expression issues an error if it exists, despite the deferred type.
        public void DeferredTypeTest_Negative(string script)
        {
            Preview.FeatureFlags.StringInterpolation = true;
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("X", FormulaType.Unknown);
            symbolTable.AddVariable("N", FormulaType.Number);
            symbolTable.AddVariable("R", RecordType.Empty());
            TestBindingError(script, symbolTable);
        }

        private void TestDeferredTypeBindingWarning(string script, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(Features.EnableDeferredType)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.True(result.IsSuccess);
            Assert.True(result.Errors.Count() > 0);
            Assert.True(result.Errors.All(error => error.MessageKey.Equals(TexlStrings.WarnUnknownType.Key)));
        }

        private void TestBindingError(string script, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig(Features.EnableDeferredType)
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.False(result.IsSuccess);
        }
    }
}
