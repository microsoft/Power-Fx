﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.Public.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class TypeTests
    {
        [Fact]
        public void RecordType()
        {
            RecordType r1 = new RecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            RecordType r2 = new RecordType()
                .Add(new NamedFormulaType("B", FormulaType.Boolean))
                .Add(new NamedFormulaType("Num", FormulaType.Number));

            // Structural equivalence
            Assert.Equal(r1, r2);

            // Test op==
            Assert.True(r1 == r2);
            Assert.False(null == r2);
            Assert.False(r1 == null);

            Assert.True(r1 != null);
            Assert.True(null != r1);
            Assert.False(r1 != r2);

            Assert.True(r1.Equals(r2));
            Assert.False(r1.Equals(null));


            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        }        
    }
}
