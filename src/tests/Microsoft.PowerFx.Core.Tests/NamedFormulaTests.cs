// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulaTests
    {
        [Theory]
        [InlineData("x=1;y=2;")]
        public void NamedFormulaTest(string script)
        {
            var namedFormula = new NamedFormula(script);
            Assert.Equal(script, namedFormula.Script);
        }

        [Theory]
        [InlineData("x=1;y=2;")]
        public void EnsureParsedTest(string script)
        {
            var namedFormula = new NamedFormula(script);
            Assert.Null(namedFormula.FormulasResult);

            namedFormula.EnsureParsed();
            Assert.NotNull(namedFormula.FormulasResult);
        }

        [Theory]
        [InlineData("x=1;y=2;", "1", "2")]
        public void GetSubScriptTest(string script, string expectedX, string expectedY)
        {
            var namedFormula = new NamedFormula(script);
            namedFormula.EnsureParsed();
            var keys = namedFormula.FormulasResult.Keys;
            var subScripts = new List<string>();

            foreach (var name in keys)
            {
                namedFormula.TryGetSubscript(name, out var subScript);
                subScripts.Add(subScript);
            }

            Assert.Equal(expectedX, subScripts[0]);
            Assert.Equal(expectedY, subScripts[1]);
        }

        [Theory]
        [InlineData("x=1;y=2;", "Not A Valid Name")]
        public void GetSubScriptTestNegative(string script, string invalidName)
        {
            var namedFormula = new NamedFormula(script);
            namedFormula.EnsureParsed();

            namedFormula.TryGetSubscript(new Utils.DName(invalidName), out var subScript);
            Assert.Null(subScript);
        }
    }
}
