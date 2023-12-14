// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FunctionInfoTests
    {
        [Fact]
        public void FunctionInfo()
        {
            // Get any function info. 
            var engine = new Engine();
            var infos = engine.FunctionInfos.ToArray();
            var infoAbs = infos.Where(info => info.Name == "Mid").First();

            Assert.Equal("Returns the characters from the middle of a text value, given a starting position and length.", infoAbs.Description);
            Assert.Equal(2, infoAbs.MinArity);
            Assert.Equal(3, infoAbs.MaxArity);
            Assert.Equal("Mid", infoAbs.Name);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkId=722347#m", infoAbs.HelpLink);
        }
    }
}
