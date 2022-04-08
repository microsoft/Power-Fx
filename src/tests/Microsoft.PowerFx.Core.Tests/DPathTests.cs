// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DPathTest 
    {
        [Fact]
        public void TestBasicDPath()
        {
            var path1 = DPath.Root.Append(new DName("A")).Append(new DName("B"));

            Assert.Equal("A", path1.Parent.Name.Value);
            Assert.False(path1.Parent.Parent.Name.IsValid);
            Assert.True(path1.Parent.Parent.IsRoot);
            Assert.Equal("A", path1[0].Value);
            Assert.Equal("B", path1[1].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => path1[2]);
            Assert.Throws<ArgumentOutOfRangeException>(() => path1[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => path1[5555]);
            Assert.Throws<ArgumentOutOfRangeException>(() => DPath.Root[0]);
        }
        
        [Fact]
        public void TestInvalidAppendThrows()
        {
            Assert.Throws<ArgumentException>(() => DPath.Root.Append(default(DName)));
        }

        [Theory]
        [InlineData("'Some NameSpace'.'foo''''bar'", "Some NameSpace", "foo''bar")]
        [InlineData("a.b.c", "a", "b", "c")]
        [InlineData("a.a.a.a.a.a.a.a", "a", "a", "a", "a", "a", "a", "a", "a")]
        [InlineData("a", "a")]
        [InlineData("'a.a.a'.b", "a.a.a", "b")]
        [InlineData("'!@#$%^'.'&*()_'", "!@#$%^", "&*()_")]
        public void DPathConstruction(string expectedPath, params string[] segments)
        {
            var path = DPath.Root;
            foreach (var name in segments)
            {
                path = path.Append(new DName(name));
            }

            Assert.Equal(expectedPath, path.ToString());
            Assert.Equal(segments.ToList(), path.Segments().Select(segment => segment.Value).ToList());
        }
    }
}
