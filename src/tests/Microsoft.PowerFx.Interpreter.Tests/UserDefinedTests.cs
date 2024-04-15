// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class UserDefinedTests
    {
        [Theory]
        [InlineData("x=1;y=2;z=x+y;", "Float(Abs(-(x+y+z)))", 6d)]
        [InlineData("x=1;y=2;Foo(x: Number): Number = Abs(x);", "Foo(-(y*y)+x)", 3d)]
        [InlineData("myvar=Weekday(Date(2024,2,2)) > 1 And false;Bar(x: Number): Number = x + x;", "Bar(1) + myvar", 2d)]
        public void NamedFormulaEntryTest(string script, string expression, double expected)
        {
            var engine = new RecalcEngine();

            engine.AddUserDefinitions(script);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = (NumberValue)check.GetEvaluator().Eval();
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) };")]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(abc, { bcd: 1 }) }; num = 3;")]
        public void ValidUDFBodyTest(string script)
        {
            var result = UserDefinitions.Process(script, null);

            Assert.False(result.HasErrors);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b);")]
        public void InvalidUDFBodyTest(string script)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = false,
                Culture = CultureInfo.InvariantCulture
            };
            UserDefinitions.ProcessUserDefinitions(script, options, out var result);

            Assert.True(result.HasErrors);
        }

        [Theory]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;}; num = 3;", 1, 1)]
        [InlineData("test2(b: Boolean): Boolean = { Set(a, b); Collect(,) ;};;;;;;;; num = 3;", 1, 1)]
        public void InvalidUDFBodyTest2(string script, int udfCount, int nfCount)
        {
            var options = new ParserOptions()
            {
                AllowsSideEffects = false,
                Culture = CultureInfo.InvariantCulture
            };
            UserDefinitions.ProcessUserDefinitions(script, options, out var result);

            Assert.True(result.HasErrors);
            Assert.Equal(udfCount, result.UDFs.Count());
            Assert.Equal(nfCount, result.NamedFormulas.Count());
        }
    }
}
