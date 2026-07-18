// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Microsoft.PowerFx.Performance.Tests
{
    // ETW and native profiler support stays opt-in so routine benchmark runs do not need elevation.
    [MemoryDiagnoser]
    [CategoriesColumn]
    [MinColumn]
    [Q1Column]
    [MeanColumn]
    [MedianColumn]
    [Q3Column]
    [MaxColumn]
    [BenchmarkCategory("ParsingAndBinding")]
    public class ParsingAndBindingPerformance
    {
        private Engine _engine;
        private ParseResult _parseResult;

        [ParamsSource(nameof(Scenarios))]
        public FormulaBenchmarkScenario Scenario { get; set; }

        public IEnumerable<FormulaBenchmarkScenario> Scenarios => FormulaBenchmarkScenarios.Create();

        [GlobalSetup]
        public void GlobalSetup()
        {
            _engine = new Engine(new PowerFxConfig(Features.PowerFxV1));
            _parseResult = _engine.Parse(Scenario.Expression, Scenario.ParserOptions);

            var bind = Bind();
            var check = Check();

            ValidateScenario(bind, check);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Parse")]
        public ParseResult Parse()
        {
            return _engine.Parse(Scenario.Expression, Scenario.ParserOptions);
        }

        [Benchmark]
        [BenchmarkCategory("Bind")]
        public CheckResult Bind()
        {
            // SetText(ParseResult) reuses the parsed tree but does not copy parse errors into the new CheckResult, so
            // parse-error scenarios must also surface binding errors; GlobalSetup validates that contract here.
            var result = new CheckResult(_engine)
                .SetText(_parseResult)
                .SetBindingInfo(Scenario.Symbols);

            result.ApplyBinding();
            return result;
        }

        [Benchmark]
        [BenchmarkCategory("Check")]
        public CheckResult Check()
        {
            // Check includes error processing and dependency analysis in addition to parse and binding, so this is
            // the end-to-end public API measurement rather than a simple Parse+Bind sum.
            return _engine.Check(Scenario.Expression, Scenario.ParserOptions, Scenario.Symbols);
        }

        private void ValidateScenario(CheckResult bind, CheckResult check)
        {
            var parseSuccess = _parseResult.IsSuccess;
            var bindSuccess = bind.IsSuccess;
            var checkSuccess = check.IsSuccess;

            var matched = Scenario.ExpectedOutcome switch
            {
                FormulaBenchmarkExpectedOutcome.Success => parseSuccess && bindSuccess && checkSuccess,
                FormulaBenchmarkExpectedOutcome.ParseError => !parseSuccess && !bindSuccess && !checkSuccess,
                FormulaBenchmarkExpectedOutcome.BindError => parseSuccess && !bindSuccess && !checkSuccess,
                _ => false,
            };

            if (matched)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Scenario '{Scenario.Name}' expected {Scenario.ExpectedOutcome}; actual parse={parseSuccess}, bind={bindSuccess}, check={checkSuccess}; " +
                $"parse errors: {FormatErrors(_parseResult.Errors)}; " +
                $"bind errors: {FormatErrors(bind.Errors)}; " +
                $"check errors: {FormatErrors(check.Errors)}.");
        }

        private static string FormatErrors(IEnumerable<ExpressionError> errors)
        {
            return string.Join(" | ", errors.Select(error => error.Message));
        }
    }
}
