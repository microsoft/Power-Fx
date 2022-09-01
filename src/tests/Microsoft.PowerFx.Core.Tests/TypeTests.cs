// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
    }
}
