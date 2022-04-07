// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public sealed class DNameTests
    {
        [Fact]
        public void TestMakeValid()
        {
            DName name;
            bool modified;

            name = DName.MakeValid(" Name   ", out modified);
            Assert.False(modified);
            Assert.Equal(" Name   ", name);

            name = DName.MakeValid("Name\bAbcd", out modified);
            Assert.False(modified);
            Assert.Equal("Name\bAbcd", name);

            name = DName.MakeValid("Name\nAbcd", out modified);
            Assert.True(modified);
            Assert.Equal("Name Abcd", name);

            name = DName.MakeValid("   ", out modified);
            Assert.True(modified);
            Assert.Equal("_   ", name);

            name = DName.MakeValid(" \t ", out modified);
            Assert.True(modified);
            Assert.Equal("_   ", name);
        }
    }
}
