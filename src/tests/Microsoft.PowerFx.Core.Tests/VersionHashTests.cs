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

            // Compare to non VersionHash
            Assert.False(v2.Equals(3));
            Assert.False(v2.Equals(null));
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

        // Hash doesn't *need* to be unique, but it is highly desirably to help catch checks. 
        [Fact]
        public void Unique()
        {
            var set = new HashSet<VersionHash>();
            for (int i = 0; i < 10 * 1000; i++)
            {
                // Create 2 in rapid succession to look for timer issues. 
                var v1 = VersionHash.New();
                var v2 = VersionHash.New();

                Assert.NotEqual(v1, v2);

                // Ensure these are unique with all previous ones. 
                Assert.True(set.Add(v1));
                Assert.True(set.Add(v2));

                // Incremented values should also be unique. 
                for (int j = 0; j < 10; j++)
                {
                    v1.Inc();
                    Assert.True(set.Add(v1));
                }
            }
        }
    }
}
