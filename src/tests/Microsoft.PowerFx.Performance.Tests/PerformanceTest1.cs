// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Performance.Tests
{
    [EtwProfiler]
    [MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn]
    public class PerformanceTest1 : PowerFxTest
    {
        private RecordValue record;
        private SymbolTable symbolTable;
        private ISymbolSlot slot;
        private Engine engine;

        [GlobalSetup]
        public void GlobalSetup()
        {
            record = FormulaValue.NewRecordFromFields(new NamedValue("y", FormulaValue.New(11)));
            symbolTable = new SymbolTable { DebugName = "PerformanceTest1" };
            slot = symbolTable.AddVariable("x", record.Type);
            engine = new Engine(new PowerFxConfig(new CultureInfo("en-US"), Features.All));
        }

        [Benchmark]
        public CheckResult Check()
        {
            var expr = "Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1) + Sum(1)";
            var check = engine.Check(expr, symbolTable: symbolTable);
            return check;
        }
    }
}
