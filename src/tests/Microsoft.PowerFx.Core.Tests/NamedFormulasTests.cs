// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulasTests : PowerFxTest
    {
        [Theory]
        [InlineData("Foo(x: Number): Number => Abs(x);")]
        public void DefFuncTest(string script)
        {
            var parsedUDFS = new ParsedUDFs(script);
            var result = parsedUDFS.GetParsed();
            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("Abs(x)", udf.Body.ToString());
            Assert.Equal("Number", udf.ReturnType.ToString());
            var arg = udf.Args.First();
            Assert.Equal("x", arg.VarIdent.ToString());
            Assert.Equal("Number", arg.VarType.ToString());
        }

        [Theory]
        [InlineData("Rec4(x: Number): Number => { { force: 1, goo: x } };" +
                    "Rec5(x: Number): Number => { \"asfd\"; { force: 1, goo: x } };" +
                    "Rec6(x: Number): Number => x + 1;" +
                    "Rec7(x: Number): Number => { x + 1 };")]
        public void DefFunctionFromDiscussion(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();
            Assert.False(result.HasError);
        }

        [Theory]
        [InlineData("Foo(): Number => { 1+1; 2+2; };")]
        public void TestChaining(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();

            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("1 + 1 ; 2 + 2", udf.Body.ToString());
        } 

        [Theory]
        [InlineData("Foo(): Number => { Sum(1, 1); Sum(2, 2); };")]
        public void TestChaining2(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();

            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("Sum(1, 1) ; Sum(2, 2)", udf.Body.ToString());
        }

        [Theory]
        [InlineData("x=1;y=2;")]
        public void NamedFormulaTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Equal(script, namedFormula.Script);
        }

        [Theory]
        [InlineData("x=1;y=2;", 2)]
        public void EnsureParsedTest(string script, int count)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.NotNull(formulas);
            Assert.Equal(formulas.Count(), count);
        }

        [Theory]
        [InlineData("x=")]
        public void EnsureParsedWithErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.Empty(formulas);            
        }

        [Theory]
        [InlineData("x=")]
        public void GetParseErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData("x=1;")]
        public void GetParseErrorsNoErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("x=1;y=2;", "1", "2")]
        public void GetNamedFormulasTest(string script, string expectedX, string expectedY)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            formulas.OrderBy(formula => formula.formula.Script);

            Assert.NotNull(formulas);

            Assert.Equal(expectedX, formulas.ElementAt(0).formula.Script);
            Assert.Equal(expectedY, formulas.ElementAt(1).formula.Script);
        }
    }
}
