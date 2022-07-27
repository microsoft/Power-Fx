// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class VersionHashTests
    {
        [Fact]
        public void Test()
        {
            var v1 = VersionHash.New();
            var v2 = VersionHash.New();

            Assert.Equal(v1, v1);

            Assert.NotEqual(v1, v2);

            var v1Snapshot = v1;
            Assert.Equal(v1, v1Snapshot);
            v1.Inc();
            Assert.NotEqual(v1, v1Snapshot);
        }

        [Fact]
        public void Equality()
        {
            var v1a = VersionHash.New();
            var v1b = v1a;

            var v2 = VersionHash.New();

            // Operator overload checks
            Assert.True(v1a == v1b);
            Assert.True(v1a != v2);

            Assert.False(v1a == v2);
            Assert.False(v1a != v1b);

            Assert.True(v1a.Equals(v1b));

            Assert.NotEqual(v1a.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        public void Combine()
        {
            var v1 = VersionHash.New();

            var v2 = v1.Combine(v1);
            Assert.NotEqual(v1, v2);

            // An object's version hash may be computed from its dependents. 
            // (v1,vx) != (v1,vy). 
            var vx = VersionHash.New();
            var vy = VersionHash.New();

            Assert.NotEqual(v1.Combine(vx), v1.Combine(vy));
        }
    }
}
