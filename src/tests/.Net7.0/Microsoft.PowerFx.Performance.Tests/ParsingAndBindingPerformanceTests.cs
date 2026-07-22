// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
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
            var duplicates = names
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.Equal(11, scenarios.Count);
            Assert.Empty(duplicates);

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

            var expectedGlobals = Enumerable.Range(0, 1000)
                .Reverse()
                .Select(index => "Global" + index.ToString("0000", CultureInfo.InvariantCulture));
            Assert.Equal(string.Join(" + ", expectedGlobals), first["LargeGlobals"].Expression);
            Assert.Equal(first["LargeGlobals"].Expression, second["LargeGlobals"].Expression);
        }

        [Fact]
        public void ScenariosMatchExpectedOutcomes()
        {
            foreach (var scenario in FormulaBenchmarkScenarios.Create())
            {
                var benchmark = new ParsingAndBindingPerformance
                {
                    Scenario = scenario
                };

                benchmark.GlobalSetup();

                var parse = benchmark.Parse();
                var bind = benchmark.Bind();
                var check = benchmark.Check();

                // These xUnit assertions intentionally duplicate GlobalSetup validation to produce clearer failures.
                switch (scenario.ExpectedOutcome)
                {
                    case FormulaBenchmarkExpectedOutcome.Success:
                        Assert.True(parse.IsSuccess);
                        Assert.True(bind.IsSuccess);
                        Assert.True(check.IsSuccess);
                        break;
                    case FormulaBenchmarkExpectedOutcome.ParseError:
                        Assert.False(parse.IsSuccess);
                        Assert.False(bind.IsSuccess);
                        Assert.False(check.IsSuccess);
                        break;
                    case FormulaBenchmarkExpectedOutcome.BindError:
                        Assert.True(parse.IsSuccess);
                        Assert.False(bind.IsSuccess);
                        Assert.False(check.IsSuccess);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected outcome {scenario.ExpectedOutcome}.");
                }
            }
        }

        [Fact]
        public void BindCreatesFreshCheckResults()
        {
            foreach (var scenario in FormulaBenchmarkScenarios.Create())
            {
                var benchmark = new ParsingAndBindingPerformance
                {
                    Scenario = scenario
                };

                benchmark.GlobalSetup();

                var first = benchmark.Bind();
                var second = benchmark.Bind();

                Assert.NotSame(first, second);
                Assert.Same(first.Parse, second.Parse);
            }
        }
    }
}
