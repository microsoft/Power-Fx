// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class CheckResultTests
    {        
        [Theory]
        [Obsolete("Using EnableJoinFunction")]
        [InlineData(
            @"ParseJSON(""{""""a"""":1,""""c"""":true}"", Type({a:Number,c:Boolean}))",
            "ParseJSON(#$string$#, Type({ #$fieldname$#:#$firstname$#, #$fieldname$#:#$firstname$# }))", 
            true,
            @"ParseJSON(""xxxxxxxxxxxxxxxxxxxx"", Type({a:Number,c:Boolean}))")]
        public void TestApplyGetLogging(string expr, string expectedLog, bool success, string simpleAnonymized)
        {
            var config = new PowerFxConfig();
            config.EnableJoinFunction();
            var engine = new Engine(config);
            var check = new CheckResult(engine);

            Assert.Throws<InvalidOperationException>(() => check.ApplyGetLogging());
            Assert.Throws<InvalidOperationException>(() => check.ApplySimpleAnonymizer());

            // Only requires text, not binding
            check.SetText(expr);
            var log = check.ApplyGetLogging();
            Assert.Equal(success, check.IsSuccess);
            Assert.Equal(expectedLog, log);
            Assert.Equal(simpleAnonymized, check.ApplySimpleAnonymizer());
        }
    }
}
