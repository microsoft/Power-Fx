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
            var keys = namedFormula.FormulasResult.Keys;
            var subScripts = new List<string>();

            foreach (var name in keys)
            {
                namedFormula.TryGetSubscript(name, out var subScript);
                subScripts.Add(subScript);
            }

            Assert.Equal("1", subScripts[0]);
            Assert.Equal("2", subScripts[1]);
        }

        [Fact]
        public void GetSubScriptTestNegative()
        {
            var namedFormula = new NamedFormula(script);
            namedFormula.EnsureParsed();

            namedFormula.TryGetSubscript(new Utils.DName("Not A Valid Name"), out var subScript);
            Assert.Null(subScript);
        }
    }
}
