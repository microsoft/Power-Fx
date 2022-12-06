// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;

namespace Microsoft.PowerFx.Performance.Tests
{

    class Program
    {
        static void Main(string[] args)
        {
            /*
             * 
             *       This program needs to be run elevated
             *       Also set the following env. variable before starting VS
             *       SET MSBuildSDKsPath=C:\Program Files\dotnet\sdk\7.0.100\Sdks
             * 
             * 
             */


            var summary = BenchmarkRunner.Run<PerformanceTest1>();
        }
    }
}
