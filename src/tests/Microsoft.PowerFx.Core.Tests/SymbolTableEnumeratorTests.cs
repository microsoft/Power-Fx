// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class SymbolTableEnumeratorTests
    {
        [Fact]
        public void NoLoops()
        {
            var s1 = new SymbolTable
            {
                DebugName = "Alpha"
            };
            var s2 = new SymbolTable
            {
                DebugName = "Beta",
                Parent = s1
            };

            // Create a cycle
            var list = new SymbolTableEnumerator(s2, s1);
            Assert.Throws<InvalidOperationException>(() => list.ToArray());
        }

        [Fact]
        public void Traverse()
        {
            var s1 = new SymbolTable
            {
                DebugName = "1a",
                Parent = new SymbolTable
                {
                    DebugName = "1b"
                }
            };

            var s2 = new SymbolTable
            {
                DebugName = "2"
            };
                        
            // In order provided. 
            // Parents first. 
            // Skip nulls
            var list = new SymbolTableEnumerator(s1, null, s2);

            var names = string.Join(",", list.Select(x => x.DebugName));

            Assert.Equal("1a,1b,2", names);
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
