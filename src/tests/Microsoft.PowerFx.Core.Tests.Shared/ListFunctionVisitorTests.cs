// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class ListFunctionVisitorTests : PowerFxTest
    {
        [Theory]
        [InlineData("Abs(1)", "Abs", null)]
        [InlineData("Abs(Abs(Abs(Abs(Abs(1)))))", "Abs", null)]
        [InlineData("With({x:Today()}, x+1)", "With,Today", null)]
        [InlineData("SomeNameSpace.Foo() + SomeNameSpace.Bar()", "Foo,Bar", "SomeNameSpace.Foo,SomeNameSpace.Bar")]
        [InlineData("true And true", "", null)]
        [InlineData("If(true, Blank(),Error())", "If,Blank,Error", null)]
        public void ListFunctionNamesTest(string expression, string expectedNames, string expectedFullNames)
        {
            foreach (var textFirst in new bool[] { false, true })
            {
                if (textFirst)
                {
                    expression = $"={expression}";
                }

                var options = new ParserOptions() { TextFirst = textFirst };

                var engine = new Engine();
                var check = engine.Check(expression, options);
                var checkResult = new CheckResult(engine).SetText(expression, options);

                var functionsNames1 = check.GetFunctionNames();
                var functionsNames2 = checkResult.GetFunctionNames();

                var actualNames1 = string.Join(",", functionsNames1);
                var actualNames2 = string.Join(",", functionsNames2);

                Assert.Equal(expectedNames, actualNames1);
                Assert.Equal(expectedNames, actualNames2);

                if (expectedFullNames != null)
                {
                    var functionsFullNames = checkResult.GetFunctionNames(true);
                    var actualFullNames2 = string.Join(",", functionsFullNames);

                    Assert.Equal(expectedFullNames, actualFullNames2);
                }
            }
        }

        [Fact]
        public void ListFunctionNamesErrorTest()
        {
            var checkResult = new CheckResult(new Engine());
            Assert.Throws<InvalidOperationException>(() => checkResult.GetFunctionNames());
        }
    }
}
