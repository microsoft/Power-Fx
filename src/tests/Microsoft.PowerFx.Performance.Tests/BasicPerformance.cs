// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Performance.Tests
{
    [MemoryDiagnoser]
    [NativeMemoryProfiler]    
    [EtwProfiler] // https://benchmarkdotnet.org/articles/features/etwprofiler.html
    [CsvExporter] // https://benchmarkdotnet.org/articles/configs/exporters.html
    [MinColumn]
    [Q1Column]
    [MeanColumn]
    [MedianColumn]
    [Q3Column]
    [MaxColumn]
    [SimpleJob(RunStrategy.Throughput, RuntimeMoniker.NetCoreApp31, launchCount: 1, warmupCount: 2)]
    public class BasicPerformance
    {
        private PowerFxConfig _powerFxConfig;
        private Engine _engine;
        private RecalcEngine _recalcEngine;
        private ParserOptions _parserOptions;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _powerFxConfig = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
            _engine = new Engine(_powerFxConfig);
            _parserOptions = new ParserOptions() { AllowsSideEffects = true, Culture = new CultureInfo("en-US") };
            _recalcEngine = new RecalcEngine(_powerFxConfig);
        }

        [Params(1, 5, 10)]
        public int N { get; set; }

        [Benchmark]
        public IReadOnlyList<Token> Tokenize()
        {
            string expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));
            IReadOnlyList<Token> tokens = null;

            for (int i = 0; i < 80; i++)
            {
                tokens = _engine.Tokenize(expr);
            }

            return tokens;
        }

        [Benchmark]
        public ParseResult Parse()
        {
            string expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));
            ParseResult parse = null;

            for (int i = 0; i < 80; i++)
            {
                parse = _engine.Parse(expr, _parserOptions);
            }

            return parse;
        }

        [Benchmark]
        public CheckResult Check()
        {
            string expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));
            CheckResult check = null;

            for (int i = 0; i < 80; i++)
            {
                check = _engine.Check(expr);
            }

            return check;
        }

        [Benchmark]
        public FormulaValue Eval()
        {
            string expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));
            FormulaValue result = null;

            for (int i = 0; i < 80; i++)
            {
                result = _recalcEngine.Eval(expr);
            }

            return result;
        }
    }
}
