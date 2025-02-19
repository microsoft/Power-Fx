// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;
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
        [InlineData(
            @"( 1*{value : 1} ) * 2",
            "#$decimal$# * ({ #$fieldname$#:#$decimal$# }) * #$decimal$#",
            true,
            @"( 1*{value : 1} ) * 1")]

        [InlineData( @"Error(""foo"")", "Error(#$string$#)", true, @"Error(""xxx"")")] 
        [InlineData( @"""{1+2}""", "#$string$#", true, @"""xxxxx""")] 
        [InlineData( "Func1(3)", "#$function$#(#$decimal$#)", true, @"ccccc(1)")]
        public void TestApplyGetLogging(string expr, string expectedLog, bool success, string simpleAnonymized)
        {            
            var config = new PowerFxConfig();
            config.EnableJoinFunction();
            config.AddFunction(new Func1Function());
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
            Assert.Equal(simpleAnonymized, check.Parse.GetLengthConservativeAnonymizedFormula());
        }
        
        private class Func1Function : ReflectionFunction
        {
            // Must have "Execute" method. 
            public NumberValue Execute(NumberValue x)
            {
                var val = x.Value;
                return FormulaValue.New(val * 2);
            }
        }
    }
}
