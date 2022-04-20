// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Public;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FormulaSetTests
    {
        [Fact]
        public void TestFormulaSet()
        {
            var formulas = new Dictionary<string, FormulaWithParameters>()
            {
                { "D", new FormulaWithParameters("B + 1") },
                { "H", new FormulaWithParameters("A + G") },
                { "A", new FormulaWithParameters("15") },
                { "B", new FormulaWithParameters("A + 1") },
                { "E", new FormulaWithParameters("C + 1") },
                { "G", new FormulaWithParameters("F + 1") },
                { "C", new FormulaWithParameters("A + 1") },
                { "F", new FormulaWithParameters("D + E") },
            };

            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var set = new FormulaSet(engine);

            set.Add(formulas);

            var indexMap = new Dictionary<string, int>();
            var index = 0;

            foreach (var kvp in set.SortedFormulas)
            {
                indexMap[kvp.Key] = index;
                index += 1;
            }

            Assert.True(indexMap["A"] < indexMap["B"]);
            Assert.True(indexMap["A"] < indexMap["C"]);
            Assert.True(indexMap["B"] < indexMap["D"]);
            Assert.True(indexMap["C"] < indexMap["E"]);
            Assert.True(indexMap["D"] < indexMap["F"]);
            Assert.True(indexMap["E"] < indexMap["F"]);
            Assert.True(indexMap["F"] < indexMap["G"]);
            Assert.True(indexMap["A"] < indexMap["H"]);
            Assert.True(indexMap["G"] < indexMap["H"]);
        }
    }
}
