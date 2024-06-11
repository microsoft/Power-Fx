// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Shared
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
        public void ListFuntionsFromParserTests(string expression, string expected)
        {
            foreach (var textFirst in new bool[] { false, true })
            {
                if (textFirst)
                {
                    expression = $"={expression}";
                }

                var options = new ParserOptions() { TextFirst = textFirst };

                var engine = new Engine();
                var parse = engine.Parse(expression, options);
                var check = engine.Check(parse);

                // Different overloads should produce the same result.
                var functionsList1 = ListFunctionVisitor.Run(parse);
                var functionsList2 = ListFunctionVisitor.Run(check);
                var functionsList3 = ListFunctionVisitor.Run(expression, options);

                var actualStr1 = string.Join(",", functionsList1._functionNames.Select(n => n.Key));
                var actualStr2 = string.Join(",", functionsList2._functionNames.Select(n => n.Key));
                var actualStr3 = string.Join(",", functionsList3._functionNames.Select(n => n.Key));

                Assert.Equal(expected, actualStr1);
                Assert.Equal(expected, actualStr2);
                Assert.Equal(expected, actualStr3);
            }
        }
    }
}
