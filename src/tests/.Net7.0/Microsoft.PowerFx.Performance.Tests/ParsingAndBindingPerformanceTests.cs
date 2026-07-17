// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class ParsingAndBindingPerformanceTests
    {
        [Fact]
        public void ScenarioDefinitionsAreStable()
        {
            var scenarios = FormulaBenchmarkScenarios.Create();
            var names = scenarios.Select(scenario => scenario.Name).ToArray();

            Assert.Equal(10, scenarios.Count);
            Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

            Assert.All(
                scenarios,
                scenario =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Category));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Source));
                    Assert.False(string.IsNullOrWhiteSpace(scenario.Expression));
                    Assert.NotNull(scenario.ParserOptions);
                    Assert.Equal(scenario.Name, scenario.ToString());
                });
        }

        [Fact]
        public void GeneratedExpressionsAreDeterministic()
        {
            var first = FormulaBenchmarkScenarios.Create().ToDictionary(scenario => scenario.Name, StringComparer.Ordinal);
            var second = FormulaBenchmarkScenarios.Create().ToDictionary(scenario => scenario.Name, StringComparer.Ordinal);

            Assert.Equal(first["WideSwitch"].Expression, second["WideSwitch"].Expression);
            Assert.Equal(first["DeepCalls"].Expression, second["DeepCalls"].Expression);
            Assert.Equal(first["DeepScopes"].Expression, second["DeepScopes"].Expression);
            Assert.Equal("Global0999 + Global0998 + Global0997 + Global0996", first["LargeGlobals"].Expression);
        }
    }
}
