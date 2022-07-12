// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class TypeTests
    {
        [Fact]
        public void RecordType()
        {
            var r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var r2 = new RecordType()
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
    }
}
