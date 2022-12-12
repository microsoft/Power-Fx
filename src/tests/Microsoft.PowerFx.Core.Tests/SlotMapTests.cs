// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class SlotMapTests
    {
        [Fact]
        public void StaysCompact()
        {
            var map = new SlotMap<string>();

            Assert.True(map.IsEmpty);

            Add(map, "0");            
            Assert.False(map.IsEmpty);

            Add(map, "1");
                        
            map.Remove(0);
            Assert.False(map.IsEmpty);

            // Get a removed slot will fail
            var f1 = map.TryGet(0, out var value);
            Assert.False(f1);
            Assert.Null(value);

            Add(map, "0");
            
            // Empty 
            map.Remove(0);
            map.Remove(1);
            Assert.True(map.IsEmpty);
        }

        [Fact]
        public void StaysCompact2()
        {
            var map = new SlotMap<string>();

            Add(map, "0");
            Add(map, "1");
            Add(map, "2");

            map.Remove(1);
            map.Remove(0);

            Add(map, "0");
            Add(map, "1");
            Add(map, "3");
        }

        [Fact]
        public void GetFail()
        {
            var map = new SlotMap<string>();

            var f = map.TryGet(0, out var value);
            Assert.False(f);
            Assert.Null(value);
        }

        // Value should be string form of expected slot. 
        static int Add(SlotMap<string> map, string value)
        {
            var i = map.Alloc();
            map.SetInitial(i, value);

            var expectedSlot = int.Parse(value);
            Assert.Equal(expectedSlot, i);

            var f = map.TryGet(i, out var value2);
            Assert.True(f);
            Assert.Equal(value2, value);

            return i;
        }
    }
}
