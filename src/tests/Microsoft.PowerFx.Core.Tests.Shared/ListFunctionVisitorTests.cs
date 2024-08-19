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
        [InlineData("SomeNameSpace.Foo() + SomeNameSpace.Bar()", "$#CustomFunction1#$,$#CustomFunction2#$", true)]
        [InlineData("SomeNameSpace.Foo() + SomeNameSpace.Bar() + SomeNameSpace.Foo()", "$#CustomFunction1#$,$#CustomFunction2#$", true)]
        [InlineData("Foo() + Abs(1) + Foo()", "$#CustomFunction1#$,Abs", true)]
        [InlineData("true And true", "")]
        [InlineData("If(true, Blank(),Error())", "If,Blank,Error")]
        public void ListFunctionNamesTest(string expression, string expectedNames, bool annonymizeUnknownPublicFunctions = false)
        {
            foreach (var textFirst in new bool[] { false, true })
            {
                if (textFirst)
                {
                    expression = $"={expression}";
                }

                CheckFunctionNames(textFirst, expression, expectedNames, annonymizeUnknownPublicFunctions);
            }
        }

        [Theory]
        [InlineData("Hello ${Sum(1,Sqrt(2))} world", "Sum,Sqrt")]
        [InlineData("3 ' {} ${Upper(3+3)} \" ${Lower($\"{7+7}\")}", "Upper,Lower")]
        public void ListFunctionNamesTextFirstTest(string expression, string expectedNames)
        {
            CheckFunctionNames(true, expression, expectedNames, false);
        }

        [Fact]
        public void ListFunctionNamesErrorTest()
        {
            var checkResult = new CheckResult(new Engine());
            Assert.Throws<InvalidOperationException>(() => checkResult.GetFunctionNames());
        }

        private static void CheckFunctionNames(bool textFirst, string expression, string expectedNames, bool annonymizeUnknownPublicFunctions)
        {
            var options = new ParserOptions() { TextFirst = textFirst };
            var engine = new Engine();
            var check = engine.Check(expression, options);
            var checkResult = new CheckResult(engine).SetText(expression, options);

            var functionsNames1 = check.GetFunctionNames(annonymizeUnknownPublicFunctions);
            var functionsNames2 = checkResult.GetFunctionNames(annonymizeUnknownPublicFunctions);

            var actualNames1 = string.Join(",", functionsNames1);
            var actualNames2 = string.Join(",", functionsNames2);

            Assert.Equal(expectedNames, actualNames1);
            Assert.Equal(expectedNames, actualNames2);
        }
    }
}
