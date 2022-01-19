// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulaTests
    {
        private readonly string script = "x=1;y=2;";

        [Fact]
        public void NamedFormulaTest()
        {
            var namedFormula = new NamedFormula(script);

            Assert.Equal(script, namedFormula.Script);
        }

        [Fact]
        public void EnsureParsedTest()
        {
            var namedFormula = new NamedFormula(script);
            Assert.Null(namedFormula.FormulasResult);

            namedFormula.EnsureParsed();
            Assert.NotNull(namedFormula.FormulasResult);
        }

        [Fact]
        public void GetSubScriptTest()
        {
            var namedFormula = new NamedFormula(script);
            namedFormula.EnsureParsed();
            var values = namedFormula.FormulasResult.Values;
            var subscripts = new List<string>();

            foreach (var node in values)
            {
                subscripts.Add(namedFormula.GetSubscript(node));
            }

            Assert.Equal("1", subscripts[0]);
            Assert.Equal("2", subscripts[1]);
        }
    }
}
