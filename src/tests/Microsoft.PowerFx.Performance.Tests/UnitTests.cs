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
        private readonly PvaPerformance pvaPerf;

        public UnitTests()
        {
            pvaPerf = new PvaPerformance();
            pvaPerf.GlobalSetup();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineConstructorWith10KOptionSets()
        {
            pvaPerf.PvaRecalcEngineConstructorWith10KOptionSets();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineCheck()
        {
            pvaPerf.PvaRecalcEngineCheck();
        }

        [Fact]
        public void Benchmark_PvaRecalcEngineParse()
        {
            pvaPerf.PvaRecalcEngineParse();
        }
    }
}
