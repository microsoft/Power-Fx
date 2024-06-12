// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class ListFunctionVisitorTests : PowerFxTest
    {
        [Theory]
        [InlineData("Abs(1)", "Abs")]
        [InlineData("Abs(Abs(Abs(Abs(Abs(1)))))", "Abs")]
        [InlineData("With({x:Today()}, x+1)", "With,Today")]
        [InlineData("SomeNameSpace.Foo() + SomeNameSpace.Bar()", "SomeNameSpace.Foo,SomeNameSpace.Bar")]
        [InlineData("true And true", "")]
        [InlineData("If(true, Blank(),Error())", "If,Blank,Error")]
        public void ListFunctionNamesTest(string expression, string expectedNames)
        {
            foreach (var textFirst in new bool[] { false, true })
            {
                if (textFirst)
                {
                    expression = $"={expression}";
                }

                CheckFunctionNames(textFirst, expression, expectedNames);
            }
        }

        [Theory]
        [InlineData("Hello ${Sum(1,Sqrt(2))} world", "Sum,Sqrt")]
        [InlineData("3 ' {} ${Upper(3+3)} \" ${Lower($\"{7+7}\")}", "Upper,Lower")]
        public void ListFunctionNamesTextFirstTest(string expression, string expectedNames)
        {
            CheckFunctionNames(true, expression, expectedNames);
        }

        [Fact]
        public void ListFunctionNamesErrorTest()
        {
            var checkResult = new CheckResult(new Engine());
            Assert.Throws<InvalidOperationException>(() => checkResult.GetFunctionNames());
        }

        private static void CheckFunctionNames(bool textFirst, string expression, string expectedNames)
        {
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
        }
    }
}
