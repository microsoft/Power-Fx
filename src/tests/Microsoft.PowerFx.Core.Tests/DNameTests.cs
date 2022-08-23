// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public sealed class DNameTests
    {
        [Theory]
        [InlineData(" Name   ", " Name   ", false)]
        [InlineData("Name\bAbcd", "Name\bAbcd", false)]
        [InlineData("Name\nAbcd", "Name Abcd", true)]
        [InlineData("   ", "_   ", true)]
        [InlineData(" \t ", "_   ", true)]
        [InlineData("Name\u3000Abcd", "Name\u3000Abcd", false)]
        public void TestMakeValid(string name, string expected, bool expectedModified)
        {
            var result = DName.MakeValid(name, out var modified);
            Assert.Equal(modified, expectedModified);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("aA", "aA", true)]
        [InlineData("aA", "aa", false)]
        [InlineData("a", "b", false)]
        [InlineData("123", "123", true)]
        [InlineData("\n1", "1", false)]
        public void DNameEqualityTests(string name1, string name2, bool succeeds)
        {
            var dName1 = new DName(name1);
            var dName2 = new DName(name2);
            Assert.Equal(succeeds, dName1.Equals(name2));
            Assert.Equal(succeeds, dName1.Equals(dName2));
            Assert.Equal(!succeeds, name2 != dName1);
        }

        [Fact]
        public void DNameOjectEqualityTests()
        {
            Assert.False(new DName("1").Equals(1));
        }
    }
}
