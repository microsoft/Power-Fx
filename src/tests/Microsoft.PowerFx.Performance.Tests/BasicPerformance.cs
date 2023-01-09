﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
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
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
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
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var tokens = _engine.Tokenize(expr);
            return tokens;
        }

        [Benchmark]
        public ParseResult Parse()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var parse = _engine.Parse(expr, _parserOptions);
            return parse;
        }

        [Benchmark]
        public CheckResult Check()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var check = _engine.Check(expr);
            return check;
        }

        [Benchmark]
        public FormulaValue Eval()
        {
            var expr = string.Join(" + ", Enumerable.Repeat("Sum(1)", N));

            var result = _recalcEngine.Eval(expr);
            return result;
        }
    }
}
