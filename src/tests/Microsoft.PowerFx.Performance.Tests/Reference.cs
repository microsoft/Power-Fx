﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Performance.Tests
{
    [EtwProfiler] // https://benchmarkdotnet.org/articles/features/etwprofiler.html
    [CsvExporter] // https://benchmarkdotnet.org/articles/configs/exporters.html
    [MinColumn]
    [Q1Column]
    [MeanColumn]
    [MedianColumn]
    [Q3Column]
    [MaxColumn]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
    public class Reference
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public int FixedDuration()
        {
            // Fixed duration, independant of CPU speed 
            Thread.Sleep(100);
            return 0;
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public long Loop()
        {
            var j = 0;
            for (var i = 0; i < 10000000; i++)
            {
                j += i;
            }

            return j;
        }
    }
}
