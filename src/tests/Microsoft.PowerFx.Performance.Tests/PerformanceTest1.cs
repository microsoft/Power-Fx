// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Performance.Tests
{
    [EtwProfiler] // https://benchmarkdotnet.org/articles/features/etwprofiler.html
    [CsvExporter] // https://benchmarkdotnet.org/articles/configs/exporters.html
    [MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn]
    public class PerformanceTest1
    {
        private PowerFxConfig powerFxConfig;
        private Engine engine;
        private RecalcEngine recalcEngine;
        private ParserOptions parserOptions;

        [GlobalSetup]
        public void GlobalSetup()
        {
            powerFxConfig = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
            engine = new Engine(powerFxConfig);
            parserOptions = new ParserOptions() { AllowsSideEffects = true, Culture = new CultureInfo("en-US") };
            recalcEngine = new RecalcEngine(powerFxConfig);
        }

        [Params(1, 5, 10)]
        public int N { get; set; }

        [Benchmark]
        public IReadOnlyList<Token> Tokenize()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var tokens = engine.Tokenize(expr);
            return tokens;
        }

        [Benchmark]
        public ParseResult Parse()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var parse = engine.Parse(expr, parserOptions);
            return parse;
        }

        [Benchmark]
        public CheckResult Check()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var check = engine.Check(expr);
            return check;
        }

        [Benchmark]
        public FormulaValue Eval()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var result = recalcEngine.Eval(expr);
            return result;
        }
    }
}
