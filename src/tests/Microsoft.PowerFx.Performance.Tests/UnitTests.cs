// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class UnitTests
    {
        private readonly PvaPerformance _pvaPerf;

        public UnitTests()
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
}
