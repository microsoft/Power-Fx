// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Tests
{ 
    public class PlainTextTests : PowerFxTest
    {
        [Theory]
        [InlineData("PlainText(\"Test<br id = 'br1'>break line</  BR>\")", "Test\r\nbreak line")]
        [InlineData("PlainText(\"1<br>2<li>3\")", "1\r\n2\r\n3")]
        [InlineData("PlainText(\"1<BR/>2<li/>3\")", "1\r\n2\r\n3")]
        [InlineData("PlainText(\"Test<p >double</  p><div >break line</ Div>\")", "Test\r\n\r\ndouble\r\n\r\nbreak line")]
        [InlineData("PlainText(\"1<div>2<p>3<tr>4\")", "1\r\n\r\n2\r\n\r\n3\r\n\r\n4")]
        [InlineData("PlainText(\"1<Div/>2<p/>3<Tr/>4\")", "1\r\n\r\n2\r\n\r\n3\r\n\r\n4")]
        [InlineData("PlainText(\"1<  br  >2<  Li />3\")", "1\r\n2\r\n3")]
        [InlineData("PlainText(\"1<div  class = 'div2'>2< P />3<  tr>4\")", "1\r\n\r\n2\r\n\r\n3\r\n\r\n4")]
        public async Task PlainTextFunctionTest(string inputExp, string expectedResult)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var rc = new RuntimeConfig();

            var check = engine.Check(inputExp);

            Assert.True(check.IsSuccess);
            
            var result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);
            Assert.Equal(expectedResult, result.ToObject());
        }
    }
}
