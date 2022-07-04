// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class ErrorResourceKeyTests
    {
        [Fact]
        public void ErrorResourceKey_Equals()
        {
            var a = new ErrorResourceKey("key");
            var b = new ErrorResourceKey("key");

            Assert.True(a == b);
            Assert.True(a.Equals(b));
            Assert.True(b == a);
            Assert.True(b.Equals(a));

            Assert.False(a != b);
            Assert.False(b != a);
        }

        [Fact]
        public void ErrorResourceKey_NotEquals()
        {
            var a = new ErrorResourceKey("key1");
            var b = new ErrorResourceKey("key2");

            Assert.False(a == b);
            Assert.False(a.Equals(b));
            Assert.False(b == a);
            Assert.False(b.Equals(a));

            Assert.True(a != b);
            Assert.True(b != a);
        }
    }
}
