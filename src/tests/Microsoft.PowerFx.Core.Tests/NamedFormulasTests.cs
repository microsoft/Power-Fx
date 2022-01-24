// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulasTests
    {
        [Theory]
        [InlineData("x=1;y=2;")]
        public void NamedFormulaTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Equal(script, namedFormula.Script);
        }

        [Theory]
        [InlineData("x=1;y=2;")]
        public void EnsureParsedTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Null(namedFormula.FormulasResult);

            Assert.True(namedFormula.EnsureParsed());
            Assert.NotNull(namedFormula.FormulasResult);
        }

        [Theory]
        [InlineData("x=")]
        public void EnsureParsedWithErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Null(namedFormula.FormulasResult);
            Assert.False(namedFormula.EnsureParsed());            
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
        public void GetFormulasTest(string script, string expectedX, string expectedY)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();
            var formulas = namedFormula.GetFormulas();

            Assert.NotNull(formulas);
            Assert.Equal(expectedX, formulas[0].Script);
            Assert.Equal(expectedY, formulas[1].Script);
        }

        [Theory]
        [InlineData("x=1;y=2;", "Not a correct script")]
        public void GetFormulasTestNegative(string script, string invalidSubscript)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();
            var formulas = namedFormula.GetFormulas();

            Assert.NotNull(formulas);
            Assert.NotEqual(invalidSubscript, formulas[0].Script);
            Assert.NotEqual(invalidSubscript, formulas[1].Script);
        }
    }
}
