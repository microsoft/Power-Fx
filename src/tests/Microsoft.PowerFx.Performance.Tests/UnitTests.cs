// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class UnitTests_Pva
    {
        private readonly PvaPerformance _pvaPerf;

        public UnitTests_Pva()
        {
            _pvaPerf = new PvaPerformance();
            _pvaPerf.GlobalSetup();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineConstructorWith10KOptionSets()
        {
            _pvaPerf.PvaRecalcEngineConstructorWith10KOptionSets();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineCheck()
        {
            _pvaPerf.PvaRecalcEngineCheck();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineParse()
        {
            _pvaPerf.PvaRecalcEngineParse(); 
        }
    }

    public class UnitTests_Basic
    {
        private readonly BasicPerformance _basicPerformance;

        public UnitTests_Basic()
        {
            _basicPerformance = new BasicPerformance() { N = 5 };
            _basicPerformance.GlobalSetup();
        }

        [Fact]
        public void Benchmark_Tokenize()
        {
            _basicPerformance.Tokenize();
        }

        [Fact]
        public void Benchmark_Check()
        {
            _basicPerformance.Check();
        }

        [Fact]
        public void Benchmark_Parse()
        {
            _basicPerformance.Parse();
        }

        [Fact]
        public void Benchmark_Eval()
        {
            _basicPerformance.Eval();
        }
    }
}
