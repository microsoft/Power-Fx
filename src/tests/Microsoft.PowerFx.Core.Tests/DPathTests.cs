// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DPathTest 
    {
        [Fact]
        public void TryParseEmptyString()
        {
            DPath result;
            Assert.True(DPath.TryParse("", out result));
            Assert.Equal(DPath.Root, result);
        }    

        [Fact]
        public void TestDPath()
        {
            DPath path1 = DPath.Root.Append(new DName("A")).Append(new DName("B"));

            Assert.True(path1.GoUp(1).Name == "A");
            Assert.False(path1.GoUp(2).Name.IsValid);
        }
    }
}
