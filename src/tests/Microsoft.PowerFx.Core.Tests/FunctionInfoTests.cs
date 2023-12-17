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
            var infoMid = infos.Where(info => info.Name == "Mid").First();

            Assert.Equal("Returns the characters from the middle of a text value, given a starting position and length.", infoMid.Description);
            Assert.Equal(2, infoMid.MinArity);
            Assert.Equal(3, infoMid.MaxArity);
            Assert.Equal("Mid", infoMid.Name);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkId=722347#m", infoMid.HelpLink);
        }
    }
}
