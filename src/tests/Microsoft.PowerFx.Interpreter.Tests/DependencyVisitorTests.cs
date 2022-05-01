// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class DependencyFinderTests : PowerFxTest
    {
        [Theory]
        [InlineData("A + 3 + B + B", "A,B")]
        [InlineData("Filter(Accounts, Age>30)", "Accounts")] // record scope not included
        [InlineData("Filter(Accounts, ThisRecord.Age>30)", "Accounts")] // ThisRecord is implicit
        [InlineData("Filter(Accounts, Age>B)", "Accounts,B")] // captures
        [InlineData("With({B:15}, B> A)", "A")] // B is shadowed
        public void T1(string expr, string dependsOn)
        {
            // var expected = new HashSet<string>(dependsOn.Split(','));

            var engine = new RecalcEngine();

            var accountType = new TableType()
                .Add(new NamedFormulaType("Age", FormulaType.Number));

            var type = new RecordType()
                .Add(new NamedFormulaType("A", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Number))
                .Add(new NamedFormulaType("Accounts", accountType));
            var result = engine.Check(expr, type);

            Assert.True(result.IsSuccess);

            // sets should be equal
            var sorted = result.TopLevelIdentifiers.OrderBy(x => x).ToArray();
            var actualStr = string.Join(',', sorted);

            Assert.Equal(dependsOn, actualStr);
        }
    }
}
