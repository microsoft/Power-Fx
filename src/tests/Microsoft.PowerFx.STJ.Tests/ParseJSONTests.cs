// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.STJ.Tests
{
    public class ParseJSONTests
    {
        [Fact]
        public void BasicParseJson()
        {
            var config = new PowerFxConfig();
            config.EnableParseJSONFunction();
            var engine = new RecalcEngine(config);
            var result = engine.Eval("Value(ParseJSON(\"5\"))");
            Assert.Equal(5d, result.ToObject());
        }
    }
}
