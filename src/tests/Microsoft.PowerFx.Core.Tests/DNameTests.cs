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
    }
}
