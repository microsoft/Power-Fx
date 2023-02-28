// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;

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
    [SimpleJob(RunStrategy.Throughput, RuntimeMoniker.NetCoreApp31, launchCount: 1, warmupCount: 10, targetCount: 10, invocationCount: 50)]
    public class PvaPerformance
    {
        private const int ExpressionNumber = 100;

        private ParserOptions _parserOptions;
        private RecalcEngine _engine;
        private string[] _expressions;
        private Random _rnd;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _parserOptions = new ParserOptions() { AllowsSideEffects = true, Culture = new CultureInfo("en-US") };

            PowerFxConfig powerFxConfig = new PowerFxConfig(new CultureInfo("en-US"), Features.All);

            for (int i = 0; i < 10000; i++)
            {
                TestOptionSetDisplayNameProvider nameProvider = new TestOptionSetDisplayNameProvider(Enumerable.Range(0, 10).Select(j => new TestOption($"Logical{j}", $"Display{j}")).ToImmutableArray());
                powerFxConfig.AddOptionSet(new OptionSet($"OptionSet{i:0000}", nameProvider));
            }

            _engine = new RecalcEngine(powerFxConfig);
            _rnd = new Random();

            _expressions = new string[ExpressionNumber];

            for (int i = 0; i < ExpressionNumber; i++)
            {
                _expressions[i] = GetRandomExpression();
            }
        }

        [Benchmark]
        public RecalcEngine PvaRecalcEngineConstructorWith10KOptionSets()
        {
            PowerFxConfig powerFxConfig = new PowerFxConfig(new CultureInfo("en-US"), Features.All);
            RecalcEngine engine = null;

            for (int i = 0; i < 10000; i++)
            {
                TestOptionSetDisplayNameProvider nameProvider = new TestOptionSetDisplayNameProvider(Enumerable.Range(0, 10).Select(j => new TestOption($"Logical{j}", $"Display{j}")).ToImmutableArray());
                powerFxConfig.AddOptionSet(new OptionSet($"OptionSet{i:0000}", nameProvider));
            }            

            for (int i = 0; i < 1000; i++)
            {
                engine = new RecalcEngine(powerFxConfig);
            }

            return engine;
        }

        [Benchmark]
        public ParseResult PvaRecalcEngineParse()
        {
            string expression = _expressions[_rnd.Next(0, ExpressionNumber)];
            ParseResult parseResult = null;

            for (int i = 0; i < 1000; i++)
            {
                parseResult = _engine.Parse(expression, _parserOptions);

                if (!parseResult.IsSuccess)
                {
                    throw new Exception($"{expression}\r\n{string.Join("\r\n", parseResult.Errors.Select(ee => $"{ee.Message}"))}");
                }
            }

            return parseResult;
        }

        [Benchmark]
        public CheckResult PvaRecalcEngineCheck()
        {
            string expression = _expressions[_rnd.Next(0, ExpressionNumber)];
            CheckResult checkResult = null;

            for (int i = 0; i < 1000; i++)
            {
                checkResult = _engine.Check(expression, _parserOptions);

                if (!checkResult.IsSuccess)
                {
                    throw new Exception($"{expression}\r\n{string.Join("\r\n", checkResult.Errors.Select(ee => $"{ee.Message}"))}");
                }
            }

            return checkResult;
        }

        private static string GetRandomExpression()
        {
            Random rnd = new Random();
            StringBuilder expr = new StringBuilder(1024);

            for (int i = 0; i < 10; i++)
            {
                int j = rnd.Next(0, 10000); // [0 - 9999]
                int k = rnd.Next(0, 10);    // [0 -9]
                int l = rnd.Next(0, 10);    // [0 -9]

                expr.Append($"(OptionSet{j:0000}.Logical{k} ");
                expr.Append(rnd.Next() > (1 << 30) ? "=" : "<>");
                expr.Append($" OptionSet{j:0000}.Logical{l})");

                if (i != 9)
                {
                    expr.Append(rnd.Next() > (1 << 30) ? " && " : " || ");
                }
            }

            string e = expr.ToString();
            return e;
        }
    }
    
    public sealed class TestOption 
    {
        public TestOption(string value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public string Value { get; }

        public string DisplayName { get; }
    }

    public class TestOptionSetDisplayNameProvider : DisplayNameProvider
    {
        private readonly ImmutableArray<TestOption> _options;

        public TestOptionSetDisplayNameProvider(ImmutableArray<TestOption> options)
        {
            _options = options;
        }

        public override IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs
        {
            get
            {
                foreach (var option in _options)
                {
                    yield return new KeyValuePair<DName, DName>(new DName(option.Value), new DName(option.DisplayName ?? option.Value));                    
                }
            }
        }

        public override bool TryGetDisplayName(DName logicalName, out DName displayName)
        {            
            foreach (var option in _options)
            {
                if (logicalName.Value.Equals(option.Value))
                {
                    displayName = new DName(option.DisplayName ?? option.Value);
                    return true;
                }
            }

            displayName = new DName();
            return false;
        }

        public override bool TryGetLogicalName(DName displayName, out DName logicalName)
        {           
            foreach (var option in _options)
            {
                if ((option.DisplayName != null && displayName.Value.Equals(option.DisplayName)) || option.Value.Equals(displayName.Value))
                {
                    logicalName = new DName(option.Value);
                    return true;
                }
            }

            logicalName = new DName();
            return false;
        }
    }
}
