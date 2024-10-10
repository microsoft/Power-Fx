// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.PowerFx.Repl.Tests
{
    public class ModuleIdentityTests
    {
        [Fact]
        public void Collections()
        {
            var id1a = ModuleIdentity.FromFile(@"z:\path1.txt");
            var id1b = ModuleIdentity.FromFile(@"z:\Path1.txt");

            var id2 = ModuleIdentity.FromFile(@"z:\path2.txt");

            Assert.Equal(id1a, id1a); // identity 
            Assert.Equal(id1a.GetHashCode(), id1a.GetHashCode()); 

            Assert.NotEqual(id1a, id2);

            // paths should be normalized 
            Assert.Equal(id1a, id1b);

            // Work with collections.
            var set = new HashSet<ModuleIdentity>();

            var added = set.Add(id1a);
            Assert.True(added);

            added = set.Add(id1a);
            Assert.False(added);

            added = set.Add(id1b);
            Assert.False(added);

            Assert.Contains(id1a, set);
            Assert.Contains(id1b, set);

            Assert.DoesNotContain(id2, set);

            added = set.Add(id2);
            Assert.True(added);
        }

        // Ensure we normalize file paths, particularly when using ".."
        [Fact]
        public void Canonical()
        {
            var id1a = ModuleIdentity.FromFile(@"z:\foo\path1.txt");
            var id1b = ModuleIdentity.FromFile(@"z:\foo\bar\..\Path1.txt");

            Assert.Equal(id1a, id1a); // identity 
        }

        [Fact]
        public void NoToString()
        {
            var id1 = ModuleIdentity.FromFile(@"z:\foo\path1.txt");

            // *Don't implement ToString* -  we want to treat identity as opauque.
            var str = id1.ToString();

            Assert.Equal("Microsoft.PowerFx.Repl.ModuleIdentity", str);
        }

        [Fact]
        public void MustBeFullPath()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleIdentity.FromFile(null));
                        
            // Must be a full path 
            Assert.Throws<ArgumentException>(() => ModuleIdentity.FromFile("path1.txt"));
            Assert.Throws<ArgumentException>(() => ModuleIdentity.FromFile(@".\path1.txt"));
            Assert.Throws<ArgumentException>(() => ModuleIdentity.FromFile(string.Empty));
        }
    }
}
