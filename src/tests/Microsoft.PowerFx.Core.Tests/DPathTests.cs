// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DPathTest : PowerFxTest
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
            // This test is skipped when not in DEBUG configurationn
            // as we rely on Contract.Asserts to throw
#if DEBUG
            Assert.Throws<ArgumentException>(() => DPath.Root.Append(default(DName)));
#endif 
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

        [Theory]
        [InlineData("a", "a", "")]
        [InlineData("a.b.c", "a.b", "c")]
        [InlineData("a.b.c.d", "a.b", "c.d")]
        [InlineData("a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.u.v.w.x.y.z", "a", "b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.u.v.w.x.y.z")]
        [InlineData("'!@#$%^'.'&*()_'.'123'", "!@#$%^.&*()_", "123")]
        public void DPathAppendTests(string expectedPath, string path1, string path2)
        {
            var dPath1 = CreateDPath(path1);
            var dPath2 = CreateDPath(path2);
            var path = dPath1.Append(dPath2);
            Assert.Equal(expectedPath, path.ToString());
        }

        [Theory]
        [InlineData("", "", true)]
        [InlineData("a", "a", true)]
        [InlineData("a.b.c", "a.b.c", true)]
        [InlineData("a.b.c", "a.b.d", false)]
        public void DPathEqualityTests(string path1, string path2, bool succeeds)
        {
            var dPath1 = CreateDPath(path1);
            var dPath2 = CreateDPath(path2);
            Assert.Equal(succeeds, dPath1.Equals(dPath2));
            Assert.Equal(!succeeds, dPath1 != dPath2);
        }

        [Fact]
        public void DPathObjectEqualsTests()
        {
            var dPath = DPath.Root;
            dPath.Append(new DName("1"));
            Assert.False(dPath.Equals(1));
            Assert.True(dPath.Equals((object)dPath));
        }

        private DPath CreateDPath(string path)
        {
            var segments = path.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var dPath = DPath.Root;
            foreach (var name in segments)
            {
                dPath = dPath.Append(new DName(name));
            }

            return dPath;
        }
    }
}
